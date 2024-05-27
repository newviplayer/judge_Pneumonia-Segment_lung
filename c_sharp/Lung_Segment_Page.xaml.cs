using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;


namespace use_lung_model_project_v2
{
    /// <summary>
    /// Lung_Segment_Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Lung_Segment_Page : Page
    {
        public Lung_Segment_Page()
        {
            InitializeComponent();
        }
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
        private async void send_file_Click(object sender, RoutedEventArgs e)
        {
            var file_name = open_file();

            string extension = Path.GetExtension(file_name);

            if (extension == ".dcm")
            {
                var dcm_image = await SendDicomFile(file_name);

                if (dcm_image != null)
                {
                    original_lung_img.Source = dcm_image;
                }

                try
                {
                    BitmapImage seg_image = await SendImage(dcm_image, extension);
                    if (seg_image != null)
                    {
                        segment_lung_img.Source = seg_image;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }

            }
            else if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png")
            {
                Mat ori_image = Cv2.ImRead(file_name);
                Mat image = new Mat();
                Cv2.Resize(ori_image, image, new OpenCvSharp.Size(256, 256));
                original_lung_img.Source = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(image);

                try
                {
                    BitmapImage seg_image = await SendImage(file_name, extension);
                    if (seg_image != null)
                    {
                        segment_lung_img.Source = seg_image;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            

        }
        private async Task<BitmapImage> SendImage(object image, string extension)
        {
            if (extension == ".dcm")
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
                            using (var response = await httpClient.PostAsync("http://localhost:8000/lung-image-mask/", content))
                            {
                                using (var stream = await response.Content.ReadAsStreamAsync())
                                {
                                    Console.WriteLine(response);
                                    Console.WriteLine(stream);
                                    // Dicom 이미지를 저장
                                    BitmapImage mask_img_Bitmap = new BitmapImage();
                                    mask_img_Bitmap.BeginInit();
                                    mask_img_Bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    mask_img_Bitmap.StreamSource = stream;
                                    mask_img_Bitmap.EndInit();
                                    mask_img_Bitmap.Freeze(); // BitmapImage를 UI 스레드 외부에서 안전하게 사용하기 위해 Freeze

                                    return mask_img_Bitmap;
                                }
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
            else if (extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg" || extension.ToLower() == ".png")
            {
                string image_path = (string)image;
                try
                {
                    var fileBytes = File.ReadAllBytes(image_path);

                    using (var httpClient = new HttpClient())
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(new MemoryStream(fileBytes)), "file", "image.jpg");

                        using (var response = await httpClient.PostAsync("http://localhost:8000/lung-image-mask/", content))
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            Console.WriteLine(response);
                            Console.WriteLine(stream);
                            // 그레이스케일 이미지를 저장
                            BitmapImage maskimgBitmap = new BitmapImage();
                            Console.WriteLine(maskimgBitmap);
                            maskimgBitmap.BeginInit();
                            maskimgBitmap.CacheOption = BitmapCacheOption.OnLoad;
                            maskimgBitmap.StreamSource = stream;
                            maskimgBitmap.EndInit();
                            maskimgBitmap.Freeze(); // BitmapImage를 UI 스레드 외부에서 안전하게 사용하기 위해 Freeze

                            return maskimgBitmap;
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

        static async Task<BitmapImage> SendDicomFile(string file_path)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(file_path);
                using (var httpClient = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        //content.Add(new StreamContent(new MemoryStream(fileBytes)), "file", "dcm.dcm");
                        content.Add(new ByteArrayContent(fileBytes), "file", "dcm.dcm");
                        // dicom 처리하는 api로 dicom파일 전송
                        using (var response = await httpClient.PostAsync("http://localhost:8000/process_dicom_file/", content))
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

    }
}
