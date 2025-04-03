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

namespace AntiStoppingBackground
{
    /// <summary>
    /// keepworking.xaml 的交互逻辑
    /// </summary>
    public partial class keepworking : Window
    {
        public keepworking()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;
        }
    }
}
