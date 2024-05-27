using Firebase.Storage;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.Win32;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Path = System.IO.Path;

namespace use_lung_model_project_v2
{
    // 폐렴 판단 Page

    public partial class Judge_PN_Page : Page
    {
        // db에 저장된 환자 정보들 datagrid에 담기 위해 만든 class
        public class Person
        {
            public string Name { get; set; }
            public string BirthDate { get; set; }
        }
        public ObservableCollection<Person> People { get; set; }

        FirestoreDb db;
        string bucketName = "process-lung.appspot.com";
        StorageClient storage;
        string user_uid = GlobalVariables.user_uid;
        

        public Judge_PN_Page()
        {
            InitializeComponent();
            load_firebase fireabse_loader = new load_firebase();
            db = fireabse_loader.GetFirestoreDb();
            storage = fireabse_loader.GetStorageClient();
            
            // datagrid와 환자 정보 연결
            People = new ObservableCollection<Person>();
            patient_datagrid.ItemsSource = People;

            // db에 저장된 환자정보들 datagrid에 load
            load_data();
        }
        // 파일 선택창 열기
        public string open_file()
        {
            string filename = "";
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true)
                {
                    filename = dlg.FileName;
                    
                }

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            return filename;
        }

        // 버튼 누르면 popup창 띄워짐. 
        // 실질적으로 popup창에서 정보 입력 후 파일 보내기 실행
        private void send_file_btn_Click(object sender, RoutedEventArgs e)
        {
            vgg_result_lb.Content = "";
            resnet_result_lb.Content = "";
            alexnet_result_lb.Content = "";

            vgg_conf_lb.Content = "";
            resnet_conf_lb.Content = "";
            alexnet_conf_lb.Content = "";

            lung_image.Source = null;

            send_popup.IsOpen = true;
            
            
        }
        // 팝업창 확인버튼 클릭
        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // 환자 이름과 생년월일 입력 받음.
            // db에 정보들 저장하기 위해.
            string patient_name = patientName.Text;
            string patient_year = patientYear.Text;

            // popup창 닫히고 파일 보내기
            send_popup.IsOpen = false;
            string file_name = open_file();

            // 확장자 확인
            string extension = Path.GetExtension(file_name);
            // dicom파일일 경우 
            // c# -> python 으로 dicom 파일을 보내고
            // python -> c# 으로 이미지를 받는 형태로 진행됨
            if (extension == ".dcm")
            {
                Console.WriteLine("dicom파일 입니다.");
                BitmapImage dcm_image;
                // python으로부터 이미지를 받는 과정
                dcm_image = await SendDicomFile(file_name);
                if (dcm_image != null)
                {
                    // 받은 이미지 imagebox에 출력.
                    lung_image.Source = dcm_image;
                }
                // 받은 이미지 firebase에 저장 
                BitmapSource bitmapSource = (BitmapSource)lung_image.Source;
                // BitmapImage resize시켜서 용량 줄이는 방법
                //
                // ----------------------------------------
                // 이미지 firebase에 저장할때 환자이름+환자생년월일 형태로 저장.
                string objectName = $"{patient_name + patient_year}.jpg";
               
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder(); // 이미지를 JPEG 형식으로 인코딩
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(memoryStream);
                    // MemoryStream을 사용하여 이미지를 Firebase Storage에 업로드
                    var imageurl = storage.UploadObject(bucketName, user_uid + "/" + objectName, "image/jpeg", memoryStream);
                }
                // 이미지 firebase에 저장 완료
                
                // vgg 모델로 이미지 분석 요청
                string vgg16_apiResponse = await SendImage_to_vgg16(dcm_image, extension);
                dynamic vgg16_data = JsonConvert.DeserializeObject(vgg16_apiResponse);
                // resnet모델로 이미지 분석 요청
                string resnet_apiResponse = await SendImage_to_resnet(dcm_image, extension);
                dynamic resnet_data = JsonConvert.DeserializeObject(resnet_apiResponse);
                // alexnet모델로 이미지 분석 요청
                string alexnet_apiResponse = await SendImage_to_alexnet(dcm_image, extension);
                dynamic alexnet_data = JsonConvert.DeserializeObject(alexnet_apiResponse);

                var vgg16_judgeres = vgg16_data.result.Value;
                var vgg16_judgeconfidence = vgg16_data.confidence.Value;
                Console.WriteLine($"vgg : {vgg16_judgeres}");
                Console.WriteLine($"vgg : {vgg16_judgeconfidence}");

                var resnet_judgeres = resnet_data.result.Value;
                var resnet_judgeconfidence = resnet_data.confidence.Value;
                Console.WriteLine($"resnet : {resnet_judgeres}");
                Console.WriteLine($"resnet : {resnet_judgeconfidence}");

                var alexnet_judgeres = alexnet_data.result.Value;
                var alexnet_judgeconfidence = alexnet_data.confidence.Value;
                Console.WriteLine($"alexnet : {alexnet_judgeres}");
                Console.WriteLine($"alexnet : {alexnet_judgeconfidence}");

                // storage에 저장된 이미지 url을 db에 저장
                string downloadUrl = await GetDownloadUrlFromFirebaseStorage(user_uid, objectName);

                // db에 분석된 데이터들 과 이미지 url 저장
                DocumentReference coll = db.Collection(user_uid).Document(patient_name + "-" + patient_year);
                Dictionary<string, object> data1 = new Dictionary<string, object>()
                {
                    {"vgg16_result", vgg16_judgeres },
                    {"vgg16_confidence" , vgg16_judgeconfidence },
                    {"resnet_result", resnet_judgeres },
                    {"resnet_confidence" , resnet_judgeconfidence },
                    {"alexnet_result", alexnet_judgeres },
                    {"alexnet_confidence", alexnet_judgeconfidence },
                    {"image_url", downloadUrl }
                };
                // 저장
                await coll.SetAsync(data1);

                // 분석된 결과 label에 띄워주기.
                vgg_result_lb.Content = vgg16_judgeres;
                vgg_conf_lb.Content = vgg16_judgeconfidence;
                resnet_result_lb.Content = resnet_judgeres;
                resnet_conf_lb.Content = resnet_judgeconfidence;
                alexnet_result_lb.Content = alexnet_judgeres;
                alexnet_conf_lb.Content = alexnet_judgeconfidence;


            }
            // jpg도 똑같은 형식으로 진행된다 
            // 다만 dicom파일과 비교했을때 이미지를 받는 과정이 생략되기 때문에 
            // imagebox에 이미지가 바로 출력되고 firebase storage에 바로 저장됨.
            else if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png")
            {
                Console.WriteLine("image파일 입니다.");

                Mat ori_image = Cv2.ImRead(file_name);
                Mat image = new Mat();
                Cv2.Resize(ori_image, image, new OpenCvSharp.Size(256, 256));
                // imagebox에 바로 출력
                lung_image.Source = BitmapSourceConverter.ToBitmapSource(image);
               
                BitmapSource bitmapSource = (BitmapSource)lung_image.Source;


                string objectName = $"{patient_name + patient_year}.jpg";
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder(); // 이미지를 JPEG 형식으로 인코딩
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(memoryStream);

                    // MemoryStream을 사용하여 이미지를 Firebase Storage에 업로드
                    var imageurl = storage.UploadObject(bucketName, user_uid + "/" + objectName, "image/jpeg", memoryStream);
                    Console.WriteLine(imageurl);
                }
                // vgg 모델로 이미지 분석 요청
                string vgg16_apiResponse = await SendImage_to_vgg16(file_name, extension);
                dynamic vgg16_data = JsonConvert.DeserializeObject(vgg16_apiResponse);
                // resnet모델로 이미지 분석 요청
                string resnet_apiResponse = await SendImage_to_resnet(file_name, extension);
                dynamic resnet_data = JsonConvert.DeserializeObject(resnet_apiResponse);
                // alexnet모델로 이미지 분석 요청
                string alexnet_apiResponse = await SendImage_to_alexnet(file_name, extension);
                dynamic alexnet_data = JsonConvert.DeserializeObject(alexnet_apiResponse);

                var vgg16_judgeres = vgg16_data.result.Value;
                var vgg16_judgeconfidence = vgg16_data.confidence.Value;
                
                var resnet_judgeres = resnet_data.result.Value;
                var resnet_judgeconfidence = resnet_data.confidence.Value;
                
                var alexnet_judgeres = alexnet_data.result.Value;
                var alexnet_judgeconfidence = alexnet_data.confidence.Value;

                Console.WriteLine($"vgg : {vgg16_judgeres}");
                Console.WriteLine($"vgg : {vgg16_judgeconfidence}");

                Console.WriteLine($"resnet : {resnet_judgeres}");
                Console.WriteLine($"resnet : {resnet_judgeconfidence}");

                Console.WriteLine($"alexnet : {alexnet_judgeres}");
                Console.WriteLine($"alexnet : {alexnet_judgeconfidence}");

                string downloadUrl = await GetDownloadUrlFromFirebaseStorage(user_uid, objectName);

                DocumentReference coll = db.Collection(user_uid).Document(patient_name + "-" + patient_year);
                Dictionary<string, object> data1 = new Dictionary<string, object>()
                {
                    {"vgg16_result", vgg16_judgeres },
                    {"vgg16_confidence" , vgg16_judgeconfidence },
                    {"resnet_result", resnet_judgeres },
                    {"resnet_confidence" , resnet_judgeconfidence },
                    {"alexnet_result", alexnet_judgeres },
                    {"alexnet_confidence", alexnet_judgeconfidence },
                    {"image_url", downloadUrl }
                };
                /// 추가
                await coll.SetAsync(data1);

                vgg_result_lb.Content = vgg16_judgeres;
                vgg_conf_lb.Content = vgg16_judgeconfidence;
                resnet_result_lb.Content = resnet_judgeres;
                resnet_conf_lb.Content = resnet_judgeconfidence;
                alexnet_result_lb.Content = alexnet_judgeres;
                alexnet_conf_lb.Content = alexnet_judgeconfidence;

            }
            // 과정이 끝나면 datagrid를 추가된 데이터도 보이게 update
            load_data();
            // textbox에 저장된 정보들 초기화.
            patientName.Text = "";
            patientYear.Text = "";
        }

        // 다이콤 파일 파이썬 api로 보내기 ( c#에선 dicom이미지를 띄우는게 불가능하다고 판단
        // 파이썬으로 다이콤파일을 보내서 파이썬에서 이미지화 한다음 c# 으로 넘겨줌 )
        static async Task<BitmapImage> SendDicomFile(string file_path)
        {
            try
            {
                // file을 Bytes 형태로 변환한 뒤 python으로 전송
                byte[] fileBytes = File.ReadAllBytes(file_path);
                using (var httpClient = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        //content.Add(new StreamContent(new MemoryStream(fileBytes)), "file", "dcm.dcm");
                        content.Add(new ByteArrayContent(fileBytes), "file", "dcm.dcm");
                        // dicom 처리하는 api로 dicom파일 전송
                        using (var response = await httpClient.PostAsync("http://localhost:8000/process_dicom_file/", content))
                        // python에서 온 data 받기.
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            Console.WriteLine(response);
                            Console.WriteLine(stream);
                            // Dicom 이미지를 저장
                            BitmapImage dicom_img_Bitmap = new BitmapImage();
                            Console.WriteLine(dicom_img_Bitmap);
                            dicom_img_Bitmap.BeginInit();
                            dicom_img_Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            dicom_img_Bitmap.StreamSource = stream;
                            dicom_img_Bitmap.EndInit();
                            dicom_img_Bitmap.Freeze(); // BitmapImage를 UI 스레드 외부에서 안전하게 사용하기 위해 Freeze

                            // image return
                            return dicom_img_Bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }
        
        static async Task<string> SendImage_to_vgg16(object image, string extension)
        {
            if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png")
            {
                string image_path = (string)image;
                try
                {
                    var fileBytes = File.ReadAllBytes(image_path);
                    Console.WriteLine(fileBytes.GetType());
                    using (var httpClient = new HttpClient())
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(new MemoryStream(fileBytes)), "file", "image.jpg");
                        using (var response = await httpClient.PostAsync("http://localhost:8000/judgePN-fromimg-with-vgg16/", content))
                        {
                            return await response.Content.ReadAsStringAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
            else if (extension.ToLower() == ".dcm")
            {
                BitmapImage bitmapimage = (BitmapImage)image;
                byte[] image_array;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapimage));
                    encoder.Save(memoryStream);
                    image_array = memoryStream.ToArray();
                }
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        Console.WriteLine("전송");
                        using (var content = new MultipartFormDataContent())
                        {
                            // 이미지 바이트 배열을 전송
                            content.Add(new StreamContent(new MemoryStream(image_array)), "file", "image.jpg");
                            Console.WriteLine("전송");
                            // HTTP POST 요청 보내기
                            using (var response = await httpClient.PostAsync("http://localhost:8000/judgePN-fromimg-with-vgg16/", content))
                            {
                                return await response.Content.ReadAsStringAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
            return null;
        }
        static async Task<string> SendImage_to_resnet(object image, string extension)
        {
            if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png")
            {
                string image_path = (string)image;
                try
                {
                    var fileBytes = File.ReadAllBytes(image_path);
                    Console.WriteLine(fileBytes.GetType());
                    using (var httpClient = new HttpClient())
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(new MemoryStream(fileBytes)), "file", "image.jpg");
                        using (var response = await httpClient.PostAsync("http://localhost:8000/judgePN-fromimg-with-resnet101/", content))
                        {
                            return await response.Content.ReadAsStringAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
            else if (extension.ToLower() == ".dcm")
            {
                BitmapImage bitmapimage = (BitmapImage)image;
                byte[] image_array;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapimage));
                    encoder.Save(memoryStream);
                    image_array = memoryStream.ToArray();
                }
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        Console.WriteLine("전송");
                        using (var content = new MultipartFormDataContent())
                        {
                            // 이미지 바이트 배열을 전송
                            content.Add(new StreamContent(new MemoryStream(image_array)), "file", "image.jpg");
                            Console.WriteLine("전송");
                            // HTTP POST 요청 보내기
                            using (var response = await httpClient.PostAsync("http://localhost:8000/judgePN-fromimg-with-resnet101/", content))
                            {
                                return await response.Content.ReadAsStringAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
            return null;
            
        }

        static async Task<string> SendImage_to_alexnet(object image, string extension)
        {
            if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png")
            {
                string image_path = (string)image;
                try
                {
                    var fileBytes = File.ReadAllBytes(image_path);
                    Console.WriteLine(fileBytes.GetType());
                    using (var httpClient = new HttpClient())
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(new MemoryStream(fileBytes)), "file", "image.jpg");
                        using (var response = await httpClient.PostAsync("http://localhost:8000/judgePN-fromimg-with-alexnet/", content))
                        {
                            return await response.Content.ReadAsStringAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
            else if (extension.ToLower() == ".dcm")
            {
                BitmapImage bitmapimage = (BitmapImage)image;
                byte[] image_array;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapimage));
                    encoder.Save(memoryStream);
                    image_array = memoryStream.ToArray();
                }
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        Console.WriteLine("전송");
                        using (var content = new MultipartFormDataContent())
                        {
                            // 이미지 바이트 배열을 전송
                            content.Add(new StreamContent(new MemoryStream(image_array)), "file", "image.jpg");
                            Console.WriteLine("전송");
                            // HTTP POST 요청 보내기
                            using (var response = await httpClient.PostAsync("http://localhost:8000/judgePN-fromimg-with-alexnet/", content))
                            {
                                return await response.Content.ReadAsStringAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
            return null;
            
        }


        // 저장된 환자정보를 통해 image 가져오기.
        public async Task<string> GetDownloadUrlFromFirebaseStorage(string uid, string objectName)
        {
            // Firebase Storage에 연결
            FirebaseStorage storage = new FirebaseStorage(bucketName);

            // 이미지가 저장된 경로
            string imagePath = uid + "/" + objectName; // 이미지가 저장된 경로를 정확하게 지정해야 합니다.

            // 이미지의 다운로드 URL 가져오기
            string downloadUrl = await storage.Child(imagePath).GetDownloadUrlAsync();

            return downloadUrl;
        }
        // datagrid update
        private async void load_data()
        {
            // clear 안해주면 기존의 데이터가 추가로 update되기 때문에 반드시 해줘야함.
            People.Clear();
            string user_uid = GlobalVariables.user_uid;

            // 사용자의 컬렉션 레퍼런스 가져오기
            CollectionReference userCollectionRef = db.Collection(user_uid);

            try
            {
                // 컬렉션에서 모든 문서 가져오기
                QuerySnapshot querySnapshot = await userCollectionRef.GetSnapshotAsync();

                // 가져온 문서들을 반복하여 처리
                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    string documentId = documentSnapshot.Id;
                    if (documentId != "name")
                    {
                        Console.WriteLine("문서 이름: " + documentId);
                        string[] split_text = documentId.Split('-');
                        string name = split_text[0];
                        string birthDate = split_text[1];
                        Console.WriteLine(name);
                        Console.WriteLine(birthDate);

                        var person = new Person
                        {
                            Name = name,
                            BirthDate = birthDate
                        };
                        
                        People.Add(person);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        // 환자 정보 검색기능.
        private void serch_textbox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(serch_textbox.Text))
            {
                patient_datagrid.ItemsSource = People;
                return;
            }

            string serchText = serch_textbox.Text;

            ObservableCollection<Person> filteredPeople = new ObservableCollection<Person>();

            foreach (Person person in People)
            {
                if (person.Name.Contains(serchText))
                {
                    filteredPeople.Add(person);
                }
            }

            patient_datagrid.ItemsSource = filteredPeople;

        }
        // datagrid 환자 더블클릭 했을때 정보 불러오기.
        private async void datagrid_mouse_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;

            // 더블 클릭된 행의 데이터 확인
            if (row != null && row.Item != null)
            {
                // 더블 클릭된 행의 데이터 가져오기
                Person clickedPerson = (Person)row.Item;

                // 더블 클릭된 행의 데이터 처리
                string name = clickedPerson.Name;
                string birthDate = clickedPerson.BirthDate;

                Console.WriteLine(user_uid);

                var vgg_confidence = "";
                var vgg_result = "";
                var resnet_confidence = "";
                var resnet_result = "";
                var alexnet_confidence = "";
                var alexnet_result = "";
                var image_url = "";

                DocumentSnapshot document = await GetDocument(user_uid, name + "-" + birthDate);
                try
                {
                    if (document != null)
                    {
                        // 문서가 존재하는 경우
                        Dictionary<string, object> data = document.ToDictionary();

                        // 데이터가 null인지 확인
                        if (data != null)
                        {
                            // 데이터가 null이 아닌 경우에만 반복문 실행
                            foreach (var item in data)
                            {
                                switch (item.Key)
                                {
                                    case "image_url":
                                        image_url = item.Value.ToString();
                                        break;
                                    case "vgg16_result":
                                        vgg_result = item.Value.ToString();
                                        break;
                                    case "vgg16_confidence":
                                        vgg_confidence = item.Value.ToString();
                                        break;
                                    case "resnet_result":
                                        resnet_result = item.Value.ToString();
                                        break;
                                    case "resnet_confidence":
                                        resnet_confidence = item.Value.ToString();
                                        break;
                                    case "alexnet_result":
                                        alexnet_result = item.Value.ToString();
                                        break;
                                    case "alexnet_confidence":
                                        alexnet_confidence = item.Value.ToString();
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        else
                        {
                            // 데이터가 null인 경우에 메시지 박스 표시
                            MessageBox.Show("문서의 데이터가 없습니다.");
                        }
                    }
                    else
                    {
                        // 문서가 존재하지 않는 경우
                        MessageBox.Show("문서를 찾을 수 없습니다.");
                    }
                }
                catch (Exception ex)
                {
                    // 예외 발생 시 메시지 박스로 예외 메시지를 표시
                    MessageBox.Show("오류 발생: " + ex.Message);
                }
                using (WebClient client = new WebClient())
                {
                    byte[] imageBytes = client.DownloadData(image_url);
                    Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
                    BitmapSource bitmapSource = BitmapSourceConverter.ToBitmapSource(image);
                    lung_image.Source = bitmapSource;
                }
                vgg_result_lb.Content = vgg_result;
                vgg_conf_lb.Content = vgg_confidence;
                resnet_result_lb.Content = resnet_result;
                resnet_conf_lb.Content = resnet_confidence;
                alexnet_result_lb.Content = alexnet_result;
                alexnet_conf_lb.Content = alexnet_confidence;

            }
        }

        private async Task<DocumentSnapshot> GetDocument(string user_uid, string documentName)
        {
            try
            {
                // 사용자의 컬렉션 레퍼런스 가져오기
                CollectionReference userCollectionRef = db.Collection(user_uid);

                // 특정 문서 가져오기
                DocumentSnapshot documentSnapshot = await userCollectionRef.Document(documentName).GetSnapshotAsync();

                return documentSnapshot;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }
        // 정보 삭제.
        public async void data_delete_btn_click(object sender, EventArgs e)
        {
            Person peopledata = patient_datagrid.SelectedItem as Person;
            if (peopledata != null)
            {
                MessageBoxResult result = MessageBox.Show("삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DocumentReference docref = db.Collection(user_uid).Document(peopledata.Name + "-" + peopledata.BirthDate);
                    DocumentSnapshot snapshot = await docref.GetSnapshotAsync();
                    if (snapshot.Exists)
                    {
                        await load_firebase.DeleteImage(user_uid, peopledata.Name+ peopledata.BirthDate);
                    }

                    await docref.DeleteAsync();
                    // 삭제된 데이터 반영해서 새로고침.
                    load_data();
                }
                else
                {
                    // nothing
                }
                                
            }
           
        }
    }
}
