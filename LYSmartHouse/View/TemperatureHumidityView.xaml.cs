using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LYSmartHouse.View
{
    /// <summary>
    /// TemperatureHumidityView.xaml 的交互逻辑
    /// </summary>
    public partial class TemperatureHumidityView : UserControl
    {
        double temperature;
        double humidity;

        public TemperatureHumidityView()
        {
            InitializeComponent();

            //sizech
        }

        Random random = new Random();

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            temperature = (int)(random.NextDouble() * 60 - 10);
            humidity = (int)(random.NextDouble() * 100);
            //humidity = 0;

            DoubleAnimation thermometerValueBarMaskAnimation = new DoubleAnimation();
            thermometerValueBarMaskAnimation.To = 2 * (50 - temperature) + 1;
            thermometerValueBarMaskAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
            thermometerValueBarMaskAnimation.DecelerationRatio = 0.5;

            ThermometerValueBarMask.BeginAnimation(Rectangle.HeightProperty, thermometerValueBarMaskAnimation);

            //Hygrometer
            DoubleAnimation hygrometerAnimation = new DoubleAnimation();
            hygrometerAnimation.To = humidity * 2.4 - 30;
            hygrometerAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
            hygrometerAnimation.DecelerationRatio = 0.5;

            HygrometerPointerRotateTransform.BeginAnimation(RotateTransform.AngleProperty, hygrometerAnimation);

            DoubleAnimation opacityAnimation = new DoubleAnimation();
            opacityAnimation.From = 0;
            opacityAnimation.To = 1;
            opacityAnimation.DecelerationRatio = 0.5;
            opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(3));

            TemperatureTextBlock.BeginAnimation(TextBlock.OpacityProperty, opacityAnimation);
            HumidityTextBlock.BeginAnimation(TextBlock.OpacityProperty, opacityAnimation);

            TemperatureTextBlock.Text = temperature.ToString();
            HumidityTextBlock.Text = humidity.ToString();
        }
    }
}
