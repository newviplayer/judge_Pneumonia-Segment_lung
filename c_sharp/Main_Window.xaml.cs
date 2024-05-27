using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
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
using System.Windows.Shapes;

namespace use_lung_model_project_v2
{
    /// <summary>
    /// Main_Window.xaml에 대한 상호 작용 논리
    /// </summary>
    
    // 기본 메인화면 정의
    public static class GlobalVariables
    {
        // user_uid 글로벌 use
        public static string user_uid { get; set; }
        // user_name 글로벌 use
        public static string user_name { get; set; }
    }
    public partial class Main_Window : Window
    {
        // 기능 사용할때 보여지는 grid
        public static Frame my_Fr { get; set; }
        // 기능 사용하기 위한 버튼 있는 grid
        public static Grid my_Grid { get; set; }
        // 로그인 하면 나오는 name
        public static Label my_name { get; set; }
        public Main_Window()
        {
            InitializeComponent();
            
            // Get
            my_Grid = my_grid;
            my_Fr = sign_inup_fr;
            my_name = user_name_lb;
            // ----
            // 맨먼저 로그인 화면이 보여야 함
            sign_inup_fr.Content = new Sign_In_Page();
        }


        // 로그인 성공하면 기능 사용 버튼 출현......
        private void to_lung_seg_Click(object sender, RoutedEventArgs e)
        {
            main_fr.Visibility = Visibility.Visible;
            Lung_Segment_Page seg_page = new Lung_Segment_Page();
            main_fr.Content = seg_page;
        }

        private void to_judge_pn_Click(object sender, RoutedEventArgs e)
        {
            main_fr.Visibility = Visibility.Visible;
            Judge_PN_Page judge_page = new Judge_PN_Page();
            main_fr.Content = judge_page;
        }

        private void logout_btn_Click(object sender, RoutedEventArgs e)
        {
            my_grid.Visibility = Visibility.Collapsed;
            main_fr.Visibility = Visibility.Collapsed;
            my_Fr.Visibility = Visibility.Visible;
            sign_inup_fr.Content = new Sign_In_Page();
        }
        // --------------------------------------------

        // 프로그램 종료 버튼...
        private void shutdown_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("종료 하시겠습니까?", "종료 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
            else
            {

            }
            
        }
        // ------------------------------------------
    }
}
