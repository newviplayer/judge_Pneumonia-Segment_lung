using Firebase.Auth;
using Firebase.Auth.Providers;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

namespace use_lung_model_project_v2
{
    
    public partial class Sign_In_Page : Page
    {
        // firebase 회원 다루는 변수
        FirebaseAuthClient client;
        // 회원 이름이 db에 저장되어있으니 그거 사용하기위해 db에 접근
        FirestoreDb db;

        public Sign_In_Page()
        {
            InitializeComponent();

            // 회원 정보 접근
            var config = load_firebase.GetFbAC();

            client = new FirebaseAuthClient(config);
            
            // 회원 이름 가져오기 위해 db에 접근
            load_firebase fireabse_loader = new load_firebase();
            db = fireabse_loader.GetFirestoreDb();
            
            
        }
        private async void login_btn_Click(object sender, RoutedEventArgs e)
        {
            string id =id_tb.Text;
            string pass = pass_tb.Password;
            
            try
            {
                // id, password 사용해서 로그인.
                var auth = await client.SignInWithEmailAndPasswordAsync(id, pass);
                // 로그인한 회원 db다루기 위해 uid 변수에 저장.
                string user_uid = auth.User.Uid.ToString();
                // 글로벌로 사용하기 위해 저장.
                GlobalVariables.user_uid = user_uid;
                
                // my_Grid는 기능 사용할때 보여져야 하는 grid
                // my_Fr은 로그인, 회원가입 할때 보여져야 하는 grid
                // 보여져야하고
                Main_Window.my_Grid.Visibility = Visibility.Visible;
                // 숨겨져야함.
                Main_Window.my_Fr.Visibility = Visibility.Collapsed;
                get_name();
            }
            // 올바른 정보가 입력되지 않았을때 MessageBox 
            catch (FirebaseAuthException ex)
            {
                AuthErrorReason reason = ex.Reason;
                Console.WriteLine(reason);
                string reasonstring = reason.ToString();
                if (reasonstring == "Unknown")
                {
                    MessageBox.Show("존재하지 않는 사용자 입니다. 이메일 혹은 비밀번호를 확인하세요.");
                }
                if (reasonstring == "InvalidEmailAddress")
                {
                    MessageBox.Show("올바른 형식의 이메일을 입력해주세요.");
                }
            }
            Console.WriteLine(id);
            
        }
        // 회원가입 하러 가기
        private void sign_up_btn_Click(object sender, RoutedEventArgs e)
        {
            Sign_Up_Page signUpPage = new Sign_Up_Page();

            Main_Window.my_Fr.Content = signUpPage;
        }

        // 사용자 이름 가져오기 위해 Db에 접근....
        private async Task<DocumentSnapshot> GetnamefromDC(string user_uid)
        {
            try
            {
                // 회원가입할때 이름을 user_uid/name 에 저장했기 때문에 거기에 접근.
                CollectionReference userCollectionRef = db.Collection(user_uid);
                DocumentSnapshot documentSnapshot = await userCollectionRef.Document("name").GetSnapshotAsync();
                return documentSnapshot;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }
        private async void get_name()
        {
            // 사용자 이름 가져오기
            DocumentSnapshot name_doc = await GetnamefromDC(GlobalVariables.user_uid);
            if (name_doc != null)
            {
                Dictionary<string, object> data = name_doc.ToDictionary();
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        // 사용자 이름 글로벌 user_name에 저장
                        GlobalVariables.user_name = item.Value.ToString();
                    }
                }
                else
                {
                    MessageBox.Show("문서의 데이터가 없습니다.");
                }
            }
            else
            {
                MessageBox.Show("문서를 찾을 수 없습니다.");
            }
            // 저장된 user_name Main_Window.my_name에 띄워주기.
            Main_Window.my_name.Content = GlobalVariables.user_name + "님 안녕하세요.";
            

        }

    }
}
