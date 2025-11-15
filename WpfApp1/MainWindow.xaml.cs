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

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private  mainview _vm;
        public MainWindow()
        {
            InitializeComponent();
            _vm = new mainview();
            DataContext = _vm;

            Loaded += async (_, __) =>
            {
                await _vm.InitializeAsync(); // 🔥 웹소켓 먼저 연결 → 방 목록 GET
            };
        }
    }
}
