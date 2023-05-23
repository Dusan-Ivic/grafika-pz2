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

namespace RG_PZ2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _startPoint = new Point();
        private Point _diffOffset = new Point();
        private double _prevOffset = 0;
        private int _zoomCurrent = 1;
        private int _zoomMax = 7;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void mainViewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                _mainViewport.CaptureMouse();

                _startPoint = e.GetPosition(this);
                _diffOffset.X = _translateTransform.OffsetX;
                _diffOffset.Y = _translateTransform.OffsetY;
            }
            else if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                _mainViewport.CaptureMouse();
            }
        }

        private void mainViewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Released)
            {
                _mainViewport.ReleaseMouseCapture();
            }
            else if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Released)
            {
                _mainViewport.ReleaseMouseCapture();
            }
        }

        private void mainViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mainViewport.IsMouseCaptured)
            {
                Point endPoint = e.GetPosition(this);
                double offsetX = endPoint.X - _startPoint.X;
                double offsetY = endPoint.Y - _startPoint.Y;

                double translateX = (offsetX * 100) / this.Width;
                double translateY = -(offsetY * 100) / this.Height;

                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    double rotationOffset = offsetY > _prevOffset ? translateY : -translateY;
                    _angleRotation.Angle = (_angleRotation.Angle + rotationOffset / 10) % 360;
                    _prevOffset = offsetY;
                }
                else
                {
                    _translateTransform.OffsetX = _diffOffset.X + (translateX / (100 * _scaleTransform.ScaleX));
                    _translateTransform.OffsetY = _diffOffset.Y + (translateY / (100 * _scaleTransform.ScaleY));
                }
            }
        }

        private void mainViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.MouseDevice.GetPosition(this);

            double scaleX, scaleY;

            if (e.Delta > 0 && _zoomCurrent < _zoomMax)
            {
                scaleX = _scaleTransform.ScaleX + 0.1;
                scaleY = _scaleTransform.ScaleY + 0.1;

                _scaleTransform.ScaleX = scaleX;
                _scaleTransform.ScaleY = scaleY;

                _zoomCurrent++;
            }
            else if (e.Delta <= 0 && _zoomCurrent > -_zoomMax)
            {
                scaleX = _scaleTransform.ScaleX - 0.1;
                scaleY = _scaleTransform.ScaleY - 0.1;

                _scaleTransform.ScaleX = scaleX;
                _scaleTransform.ScaleY = scaleY;

                _zoomCurrent--;
            }
        }

        private void resetViewportButton_Click(object sender, RoutedEventArgs e)
        {
            _startPoint = new Point();
            _diffOffset = new Point();
            _prevOffset = 0;
            _zoomCurrent = 1;

            _translateTransform.OffsetX = 0;
            _translateTransform.OffsetY = 0;
            _translateTransform.OffsetZ = 0;

            _scaleTransform.ScaleX = 1;
            _scaleTransform.ScaleY = 1;
            _scaleTransform.ScaleZ = 1;

            _angleRotation.Angle = 1;
        }
    }
}
