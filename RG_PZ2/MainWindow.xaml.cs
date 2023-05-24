using RG_PZ2.Models;
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
using System.Xml;

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

        private List<SubstationEntity> _substationEntities;
        private List<NodeEntity> _nodeEntities;
        private List<SwitchEntity> _switchEntities;
        private List<LineEntity> _lineEntities;

        public MainWindow()
        {
            _substationEntities = new List<SubstationEntity>();
            _nodeEntities = new List<NodeEntity>();
            _switchEntities = new List<SwitchEntity>();
            _lineEntities = new List<LineEntity>();

            InitializeComponent();

            LoadEntities();
        }

        #region Event Handlers

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

        #endregion Event Handlers

        private void LoadEntities()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("Geographic.xml");

            // Substations
            XmlNodeList xmlNodes = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");

            foreach (XmlNode xmlNode in xmlNodes)
            {
                SubstationEntity substationEntity = new SubstationEntity
                {
                    Id = long.Parse(xmlNode.SelectSingleNode("Id").InnerText),
                    Name = xmlNode.SelectSingleNode("Name").InnerText,
                    X = double.Parse(xmlNode.SelectSingleNode("X").InnerText, System.Globalization.CultureInfo.InvariantCulture),
                    Y = double.Parse(xmlNode.SelectSingleNode("Y").InnerText, System.Globalization.CultureInfo.InvariantCulture)
                };

                ToLatLon(substationEntity.X, substationEntity.Y, 34, out double newX, out double newY);
                substationEntity.X = newX;
                substationEntity.Y = newY;

                _substationEntities.Add(substationEntity);
            }

            // Nodes
            xmlNodes = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");

            foreach (XmlNode xmlNode in xmlNodes)
            {
                NodeEntity nodeEntity = new NodeEntity
                {
                    Id = long.Parse(xmlNode.SelectSingleNode("Id").InnerText),
                    Name = xmlNode.SelectSingleNode("Name").InnerText,
                    X = double.Parse(xmlNode.SelectSingleNode("X").InnerText, System.Globalization.CultureInfo.InvariantCulture),
                    Y = double.Parse(xmlNode.SelectSingleNode("Y").InnerText, System.Globalization.CultureInfo.InvariantCulture)
                };

                ToLatLon(nodeEntity.X, nodeEntity.Y, 34, out double newX, out double newY);
                nodeEntity.X = newX;
                nodeEntity.Y = newY;

                _nodeEntities.Add(nodeEntity);
            }

            // Switches
            xmlNodes = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");

            foreach (XmlNode xmlNode in xmlNodes)
            {
                SwitchEntity switchEntity = new SwitchEntity
                {
                    Id = long.Parse(xmlNode.SelectSingleNode("Id").InnerText),
                    Name = xmlNode.SelectSingleNode("Name").InnerText,
                    X = double.Parse(xmlNode.SelectSingleNode("X").InnerText, System.Globalization.CultureInfo.InvariantCulture),
                    Y = double.Parse(xmlNode.SelectSingleNode("Y").InnerText, System.Globalization.CultureInfo.InvariantCulture),
                    Status = xmlNode.SelectSingleNode("Status").InnerText
                };

                ToLatLon(switchEntity.X, switchEntity.Y, 34, out double newX, out double newY);
                switchEntity.X = newX;
                switchEntity.Y = newY;

                _switchEntities.Add(switchEntity);
            }

            // Lines
            xmlNodes = xmlDocument.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");

            foreach (XmlNode xmlNode in xmlNodes)
            {
                LineEntity lineEntity = new LineEntity()
                {
                    Id = long.Parse(xmlNode.SelectSingleNode("Id").InnerText),
                    Name = xmlNode.SelectSingleNode("Name").InnerText,
                    IsUnderground = bool.Parse(xmlNode.SelectSingleNode("IsUnderground").InnerText),
                    R = float.Parse(xmlNode.SelectSingleNode("R").InnerText, System.Globalization.CultureInfo.InvariantCulture),
                    ConductorMaterial = xmlNode.SelectSingleNode("ConductorMaterial").InnerText,
                    LineType = xmlNode.SelectSingleNode("LineType").InnerText,
                    ThermalConstantHeat = long.Parse(xmlNode.SelectSingleNode("ThermalConstantHeat").InnerText),
                    FirstEnd = long.Parse(xmlNode.SelectSingleNode("FirstEnd").InnerText),
                    SecondEnd = long.Parse(xmlNode.SelectSingleNode("SecondEnd").InnerText)
                };

                List<Vertex> vertices = new List<Vertex>();
                foreach (XmlNode pointNode in xmlNode.ChildNodes[9].ChildNodes)
                {
                    Vertex vertex = new Vertex()
                    {
                        X = double.Parse(pointNode.SelectSingleNode("X").InnerText, System.Globalization.CultureInfo.InvariantCulture),
                        Y = double.Parse(pointNode.SelectSingleNode("Y").InnerText, System.Globalization.CultureInfo.InvariantCulture)
                    };

                    ToLatLon(vertex.X, vertex.Y, 34, out double newX, out double newY);
                    vertex.X = newX;
                    vertex.Y = newY;

                    vertices.Add(vertex);
                }

                lineEntity.Vertices = new List<Vertex>(vertices);
                _lineEntities.Add(lineEntity);

                vertices.Clear();
            }
        }

        private void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
    }
}
