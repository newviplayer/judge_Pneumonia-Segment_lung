using Firebase.Auth;
using Firebase.Auth.Providers;
using Google.Cloud.Firestore;
using System.Windows;
using System.Windows.Controls;


namespace use_lung_model_project_v2
{
    /// <summary>
    /// Sign_Up_Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Sign_Up_Page : Page
    {
        FirebaseAuthClient client;
        FirestoreDb db;
        string user_uid;
        string email;
        public Sign_Up_Page()
        {
            InitializeComponent();

            var config = load_firebase.GetFbAC();

            client = new FirebaseAuthClient(config);
            load_firebase fireabse_loader = new load_firebase();
            db = fireabse_loader.GetFirestoreDb();
            
        }
        // 로그인창으로 되돌아 가기
        private void goto_login_Click(object sender, RoutedEventArgs e)
        {
            Sign_In_Page sign_in_page = new Sign_In_Page();
            Main_Window.my_Fr.Content = sign_in_page;
        }
        // 회원 정보 모두 입력 후 버튼누르면 회원가입 되게.
        private async void sign_up_complete_Click(object sender, RoutedEventArgs e)
        {
            email = email_tb.Text;
            string pass = pass_tb.Password;
            string pass_check = passcheck_tb.Password;
            string name = name_tb.Text;
            // 비밀번호 확인 check
            if (pass != pass_check)
            {
                MessageBox.Show("비밀번호가 다릅니다.");
            }
            else if (name == "")
            {
                MessageBox.Show("이름을 입력해주세요.");
            }
            else
            {
                try
                {
                    // Firebase에 사용자 등록
                    var auth = await client.CreateUserWithEmailAndPasswordAsync(email, pass);
                    user_uid = auth.User.Uid;
                    MessageBox.Show("회원가입 성공!");
                    Sign_In_Page sign_in_page = new Sign_In_Page();
                    Main_Window.my_Fr.Content = sign_in_page;
                    // 이름 db에 저장
                    Add_name_db(user_uid, name);
                }

                catch (FirebaseAuthException ex)
                {
                    AuthErrorReason reason = ex.Reason;
                    Console.WriteLine(reason);
                    string reasonstring = reason.ToString();
                    if (reasonstring == "InvalidEmailAddress")
                    {
                        MessageBox.Show("이메일 형식의 아이디를 사용해주세요");
                    }
                    else if (reasonstring == "WeakPassword")
                    {
                        MessageBox.Show("비밀번호 6자리 이상 입력해주세요");
                    }
                    else if (reasonstring == "EmailExists")
                    {
                        MessageBox.Show("이미 존재하는 이메일 입니다.");
                    }
                }
            }
        }
        // 입력받은 이름 회원 데이터베이스에 저장해주기
        async void Add_name_db(string user_uid, string name)
        {
            DocumentReference coll = db.Collection(user_uid).Document("name");
            Dictionary<string, object> name_db = new Dictionary<string, object>()
            {
                {"name", name },
            };

            await coll.SetAsync(name_db);

        }

       
    }
}
