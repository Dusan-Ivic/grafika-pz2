using RG_PZ2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
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

        private List<PowerEntity> _powerEntities;
        private List<SubstationEntity> _substationEntities;
        private List<NodeEntity> _nodeEntities;
        private List<SwitchEntity> _switchEntities;
        private List<LineEntity> _lineEntities;

        // Extremes
        private readonly double _minLatitude = 45.2325;
        private readonly double _maxLatitude = 45.277031;
        private readonly double _minLongitude = 19.793909;
        private readonly double _maxLongitude = 19.894459;

        // Models
        private readonly double _cubeDim = 1.5;
        private readonly double _lineDim = 1.0;
        private Dictionary<Point, List<GeometryModel3D>> _entityModelsMap;
        private List<GeometryModel3D> _entityModelsList;
        private List<Model3DGroup> _lineModelsList;
        private List<Model3DGroup> _hiddenLines;

        // Hit Testing
        private ToolTip _tooltip = new ToolTip() { IsOpen = false };
        private GeometryModel3D _lineSegmentModel;
        private GeometryModel3D _firstEndModel;
        private GeometryModel3D _secondEndModel;

        public MainWindow()
        {
            _powerEntities = new List<PowerEntity>();
            _substationEntities = new List<SubstationEntity>();
            _nodeEntities = new List<NodeEntity>();
            _switchEntities = new List<SwitchEntity>();
            _lineEntities = new List<LineEntity>();

            _entityModelsMap = new Dictionary<Point, List<GeometryModel3D>>();
            _entityModelsList = new List<GeometryModel3D>();
            _lineModelsList = new List<Model3DGroup>();
            _hiddenLines = new List<Model3DGroup>();

            InitializeComponent();

            LoadEntities();

            DrawSubstations();
            DrawNodes();
            DrawSwitches();

            DrawLines();

            ToolTipService.SetPlacement(_mainViewport, PlacementMode.Mouse);
            ToolTipService.SetToolTip(_mainViewport, _tooltip);
        }

        private void mainViewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                _mainViewport.CaptureMouse();

                _startPoint = e.GetPosition(this);
                _diffOffset.X = _translateTransform.OffsetX;
                _diffOffset.Y = _translateTransform.OffsetY;

                PointHitTestParameters hitTestParams = new PointHitTestParameters(_startPoint);
                VisualTreeHelper.HitTest(this, null, HTResult, hitTestParams);
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
            _tooltip.IsOpen = false;

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
                    _translateTransform.OffsetX = _diffOffset.X + (translateX * 80 / (100 * _scaleTransform.ScaleX));
                    _translateTransform.OffsetY = _diffOffset.Y + (translateY * 80 / (100 * _scaleTransform.ScaleY));
                }
            }
        }

        private void mainViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.MouseDevice.GetPosition(this);

            double scaleX, scaleY, scaleZ;

            if (e.Delta > 0 && _zoomCurrent < _zoomMax)
            {
                scaleX = _scaleTransform.ScaleX + 0.1;
                scaleY = _scaleTransform.ScaleY + 0.1;
                scaleZ = _scaleTransform.ScaleZ + 0.1;

                _scaleTransform.ScaleX = scaleX;
                _scaleTransform.ScaleY = scaleY;
                _scaleTransform.ScaleZ = scaleZ;

                _zoomCurrent++;
            }
            else if (e.Delta <= 0 && _zoomCurrent > -_zoomMax)
            {
                scaleX = _scaleTransform.ScaleX - 0.1;
                scaleY = _scaleTransform.ScaleY - 0.1;
                scaleZ = _scaleTransform.ScaleZ - 0.1;

                _scaleTransform.ScaleX = scaleX;
                _scaleTransform.ScaleY = scaleY;
                _scaleTransform.ScaleZ = scaleZ;

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

        private void cbHideInactiveLines_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            bool isChecked = cb.IsChecked ?? false;

            if (isChecked)
            {
                foreach (Model3DGroup lineGroup in _lineModelsList)
                {
                    LineEntity line = lineGroup.GetValue(TagProperty) as LineEntity;

                    SwitchEntity firstEnd = _switchEntities.Find(x => x.Id == line.FirstEnd);

                    if (firstEnd != null)
                    {
                        if (firstEnd.Status == "Open")
                        {
                            _modelGroup.Children.Remove(lineGroup);
                            _hiddenLines.Add(lineGroup);
                        }
                    }
                }
            }
            else
            {
                foreach (Model3DGroup lineGroup in _hiddenLines)
                {
                    _modelGroup.Children.Add(lineGroup);
                }

                _hiddenLines.Clear();
            }
        }

        private void cbColorSwitchesStatus_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            bool isChecked = cb.IsChecked ?? false;

            if (isChecked)
            {
                foreach (GeometryModel3D model in _entityModelsList)
                {
                    object entity = model.GetValue(TagProperty);

                    if (entity is SwitchEntity)
                    {
                        SwitchEntity switchEntity = entity as SwitchEntity;

                        switch (switchEntity.Status)
                        {
                            case "Open":
                                model.Material = new DiffuseMaterial(Brushes.Green);
                                break;
                            case "Closed":
                                model.Material = new DiffuseMaterial(Brushes.Red);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else
            {
                foreach (GeometryModel3D model in _entityModelsList)
                {
                    object entity = model.GetValue(TagProperty);

                    if (entity is SwitchEntity)
                    {
                        model.Material = new DiffuseMaterial(Brushes.Blue);
                    }
                }
            }
        }

        private void cbColorLinesResistance_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            bool isChecked = cb.IsChecked ?? false;

            if (isChecked)
            {
                foreach (Model3DGroup lineGroup in _lineModelsList)
                {
                    LineEntity lineEntity = lineGroup.GetValue(TagProperty) as LineEntity;

                    DiffuseMaterial material = new DiffuseMaterial();

                    if (lineEntity.R < 1)
                    {
                        material.Brush = Brushes.Red;
                    }
                    else if (lineEntity.R > 2)
                    {
                        material.Brush = Brushes.Yellow;
                    }
                    else
                    {
                        material.Brush = Brushes.Orange;
                    }

                    foreach (GeometryModel3D lineSegment in lineGroup.Children)
                    {
                        lineSegment.Material = material;
                    }
                }
            }
            else
            {
                foreach (Model3DGroup lineGroup in _lineModelsList)
                {
                    LineEntity lineEntity = lineGroup.GetValue(TagProperty) as LineEntity;

                    DiffuseMaterial material = new DiffuseMaterial();

                    switch (lineEntity.ConductorMaterial)
                    {
                        case "Steel":
                            material.Brush = Brushes.DarkSlateGray;
                            break;
                        case "Acsr":
                            material.Brush = Brushes.LightSlateGray;
                            break;
                        case "Copper":
                            material.Brush = Brushes.Brown;
                            break;
                        default:
                            break;
                    }

                    foreach (GeometryModel3D lineSegment in lineGroup.Children)
                    {
                        lineSegment.Material = material;
                    }
                }
            }
        }

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

                ToLatLon(substationEntity.X, substationEntity.Y, 34, out double latitude, out double longitude);
                substationEntity.X = longitude;
                substationEntity.Y = latitude;

                _substationEntities.Add(substationEntity);
                _powerEntities.Add(substationEntity);
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

                ToLatLon(nodeEntity.X, nodeEntity.Y, 34, out double latitude, out double longitude);
                nodeEntity.X = longitude;
                nodeEntity.Y = latitude;

                _nodeEntities.Add(nodeEntity);
                _powerEntities.Add(nodeEntity);
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

                ToLatLon(switchEntity.X, switchEntity.Y, 34, out double latitude, out double longitude);
                switchEntity.X = longitude;
                switchEntity.Y = latitude;

                _switchEntities.Add(switchEntity);
                _powerEntities.Add(switchEntity);
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

                    ToLatLon(vertex.X, vertex.Y, 34, out double latitude, out double longitude);
                    vertex.X = longitude;
                    vertex.Y = latitude;

                    vertices.Add(vertex);
                }

                lineEntity.Vertices = new List<Vertex>(vertices);
                _lineEntities.Add(lineEntity);

                vertices.Clear();
            }
        }

        private void DrawSubstations()
        {
            foreach (SubstationEntity entity in _substationEntities)
            {
                if (IsOnMap(latitude: entity.Y, longitude: entity.X))
                {
                    DrawEntity(entity);
                }
            }
        }

        private void DrawNodes()
        {
            foreach (NodeEntity entity in _nodeEntities)
            {
                if (IsOnMap(latitude: entity.Y, longitude: entity.X))
                {
                    DrawEntity(entity);
                }
            }
        }

        private void DrawSwitches()
        {
            foreach (SwitchEntity entity in _switchEntities)
            {
                if (IsOnMap(latitude: entity.Y, longitude: entity.X))
                {
                    DrawEntity(entity);
                }
            }
        }

        private void DrawEntity(PowerEntity entity)
        {
            Point point = CreatePoint(entity.X, entity.Y);

            if (!_entityModelsMap.ContainsKey(point))
            {
                _entityModelsMap.Add(point, new List<GeometryModel3D>());
            }

            MeshGeometry3D mesh = CreateCubeMesh(point);

            DiffuseMaterial material = new DiffuseMaterial();

            switch (entity.GetType().Name)
            {
                case "SubstationEntity":
                    material.Brush = Brushes.Red;
                    break;
                case "NodeEntity":
                    material.Brush = Brushes.Green;
                    break;
                case "SwitchEntity":
                    material.Brush = Brushes.Blue;
                    break;
                default:
                    break;
            }

            GeometryModel3D model = new GeometryModel3D(mesh, material);
            model.SetValue(TagProperty, entity);

            _modelGroup.Children.Add(model);

            _entityModelsMap[point].Add(model);
            _entityModelsList.Add(model);
        }

        private void DrawLines()
        {
            foreach (LineEntity line in _lineEntities)
            {
                PowerEntity firstEnd = _powerEntities.Find(e => e.Id == line.FirstEnd);
                PowerEntity secondEnd = _powerEntities.Find(e => e.Id == line.SecondEnd);

                // Ignore lines between entities that don't exist
                if (firstEnd == null || secondEnd == null)
                {
                    continue;
                }
                
                // Ignore lines between entities not on the map
                if (!IsOnMap(firstEnd.Y, firstEnd.X) || !IsOnMap(secondEnd.Y, secondEnd.X))
                {
                    continue;
                }

                DrawLine(line, firstEnd, secondEnd);
            }
        }

        private void DrawLine(LineEntity line, PowerEntity firstEnd, PowerEntity secondEnd)
        {
            DiffuseMaterial material = new DiffuseMaterial();

            switch (line.ConductorMaterial)
            {
                case "Steel":
                    material.Brush = Brushes.DarkSlateGray;
                    break;
                case "Acsr":
                    material.Brush = Brushes.LightSlateGray;
                    break;
                case "Copper":
                    material.Brush = Brushes.Brown;
                    break;
                default:
                    break;
            }

            Point startPoint = CreatePoint(firstEnd.X, firstEnd.Y);
            Point endPoint = CreatePoint(secondEnd.X, secondEnd.Y);

            Model3DGroup lineGroup = new Model3DGroup();

            for (int i = 0; i < line.Vertices.Count; i++)
            {
                Vertex vertex = line.Vertices[i];

                Point currentEndPoint = CreatePoint(vertex.X, vertex.Y);

                MeshGeometry3D mesh;

                if (i < line.Vertices.Count - 1)
                {
                    mesh = CreateLineMesh(startPoint, currentEndPoint);
                    startPoint = new Point(currentEndPoint.X, currentEndPoint.Y);
                }
                else
                {
                    mesh = CreateLineMesh(startPoint, endPoint);
                }

                GeometryModel3D model = new GeometryModel3D(mesh, material);
                model.SetValue(TagProperty, line);

                lineGroup.Children.Add(model);
            }

            lineGroup.SetValue(TagProperty, line);
            _modelGroup.Children.Add(lineGroup);
            _lineModelsList.Add(lineGroup);
        }

        private MeshGeometry3D CreateCubeMesh(Point point)
        {
            MeshGeometry3D cubeMesh = new MeshGeometry3D();

            double bottomZ = _entityModelsMap[point].Count * _cubeDim;
            double topZ = bottomZ + _cubeDim;

            Point3DCollection points = new Point3DCollection()
            {
                new Point3D(point.X - _cubeDim / 2, point.Y - _cubeDim / 2, bottomZ),
                new Point3D(point.X + _cubeDim / 2, point.Y - _cubeDim / 2, bottomZ),
                new Point3D(point.X - _cubeDim / 2, point.Y - _cubeDim / 2, topZ),
                new Point3D(point.X + _cubeDim / 2, point.Y - _cubeDim / 2, topZ),

                new Point3D(point.X - _cubeDim / 2, point.Y + _cubeDim / 2, bottomZ),
                new Point3D(point.X + _cubeDim / 2, point.Y + _cubeDim / 2, bottomZ),
                new Point3D(point.X - _cubeDim / 2, point.Y + _cubeDim / 2, topZ),
                new Point3D(point.X + _cubeDim / 2, point.Y + _cubeDim / 2, topZ)
            };

            cubeMesh.Positions = new Point3DCollection(points);

            // Bottom
            cubeMesh.TriangleIndices.Add(0);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(4);
            cubeMesh.TriangleIndices.Add(4);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(5);

            // Front
            cubeMesh.TriangleIndices.Add(3);
            cubeMesh.TriangleIndices.Add(2);
            cubeMesh.TriangleIndices.Add(0);
            cubeMesh.TriangleIndices.Add(0);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(3);

            // Back
            cubeMesh.TriangleIndices.Add(4);
            cubeMesh.TriangleIndices.Add(6);
            cubeMesh.TriangleIndices.Add(7);
            cubeMesh.TriangleIndices.Add(7);
            cubeMesh.TriangleIndices.Add(5);
            cubeMesh.TriangleIndices.Add(4);

            // Left
            cubeMesh.TriangleIndices.Add(0);
            cubeMesh.TriangleIndices.Add(2);
            cubeMesh.TriangleIndices.Add(6);
            cubeMesh.TriangleIndices.Add(6);
            cubeMesh.TriangleIndices.Add(4);
            cubeMesh.TriangleIndices.Add(0);

            // Right
            cubeMesh.TriangleIndices.Add(7);
            cubeMesh.TriangleIndices.Add(3);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(5);
            cubeMesh.TriangleIndices.Add(7);

            // Top
            cubeMesh.TriangleIndices.Add(6);
            cubeMesh.TriangleIndices.Add(2);
            cubeMesh.TriangleIndices.Add(3);
            cubeMesh.TriangleIndices.Add(3);
            cubeMesh.TriangleIndices.Add(7);
            cubeMesh.TriangleIndices.Add(6);

            // Bottom
            cubeMesh.TriangleIndices.Add(0);
            cubeMesh.TriangleIndices.Add(4);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(1);
            cubeMesh.TriangleIndices.Add(4);
            cubeMesh.TriangleIndices.Add(5);

            return cubeMesh;
        }

        private MeshGeometry3D CreateLineMesh(Point firstPoint, Point secondPoint)
        {
            MeshGeometry3D lineMesh = new MeshGeometry3D();

            double bottomZ = 0.01;
            double topZ = bottomZ + _lineDim;

            Point3DCollection points = new Point3DCollection()
            {
                new Point3D(firstPoint.X - _lineDim / 2, firstPoint.Y - _lineDim / 2, bottomZ),
                new Point3D(firstPoint.X + _lineDim / 2, firstPoint.Y + _lineDim / 2, bottomZ),
                new Point3D(firstPoint.X - _lineDim / 2, firstPoint.Y - _lineDim / 2, topZ),
                new Point3D(firstPoint.X + _lineDim / 2, firstPoint.Y + _lineDim / 2, topZ),

                new Point3D(secondPoint.X - _lineDim / 2, secondPoint.Y - _lineDim / 2, bottomZ),
                new Point3D(secondPoint.X + _lineDim / 2, secondPoint.Y + _lineDim / 2, bottomZ),
                new Point3D(secondPoint.X - _lineDim / 2, secondPoint.Y - _lineDim / 2, topZ),
                new Point3D(secondPoint.X + _lineDim / 2, secondPoint.Y + _lineDim / 2, topZ)
            };

            lineMesh.Positions = new Point3DCollection(points);

            // Front
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(1);
            lineMesh.TriangleIndices.Add(3);

            // Back
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(6);
            lineMesh.TriangleIndices.Add(7);
            lineMesh.TriangleIndices.Add(7);
            lineMesh.TriangleIndices.Add(5);
            lineMesh.TriangleIndices.Add(4);

            // Left
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(6);
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(2);

            // Right
            lineMesh.TriangleIndices.Add(5);
            lineMesh.TriangleIndices.Add(7);
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(1);
            lineMesh.TriangleIndices.Add(5);

            // Top
            lineMesh.TriangleIndices.Add(7);
            lineMesh.TriangleIndices.Add(6);
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(2);
            lineMesh.TriangleIndices.Add(3);
            lineMesh.TriangleIndices.Add(7);

            // Bottom
            lineMesh.TriangleIndices.Add(1);
            lineMesh.TriangleIndices.Add(0);
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(4);
            lineMesh.TriangleIndices.Add(5);
            lineMesh.TriangleIndices.Add(1);

            return lineMesh;
        }

        private HitTestResultBehavior HTResult(HitTestResult rawResult)
        {
            RayHitTestResult hitTestResult = rawResult as RayHitTestResult;

            if (hitTestResult != null)
            {
                Model3D hitModel = hitTestResult.ModelHit;

                object hitEntity = hitModel.GetValue(TagProperty);

                if (hitEntity is SubstationEntity)
                {
                    SubstationEntity hitSubstation = hitEntity as SubstationEntity;

                    _tooltip.Content = $"Substation\n\nID: {hitSubstation.Id}\nName: {hitSubstation.Name}";
                    _tooltip.IsOpen = true;
                }
                else if (hitEntity is NodeEntity)
                {
                    NodeEntity hitNode = hitEntity as NodeEntity;

                    _tooltip.Content = $"Node\n\nID: {hitNode.Id}\nName: {hitNode.Name}";
                    _tooltip.IsOpen = true;
                }
                else if (hitEntity is SwitchEntity)
                {
                    SwitchEntity hitSwitch = hitEntity as SwitchEntity;

                    _tooltip.Content = $"Switch\n\nID: {hitSwitch.Id}\nName: {hitSwitch.Name}\nStatus: {hitSwitch.Status}";
                    _tooltip.IsOpen = true;
                }
                else if (hitEntity is LineEntity)
                {
                    if (hitModel as GeometryModel3D != _lineSegmentModel)
                    {
                        ResetLineEndModel(_firstEndModel);
                        ResetLineEndModel(_secondEndModel);
                    }

                    LineEntity hitLine = hitEntity as LineEntity;

                    GeometryModel3D firstEndModel = _entityModelsList.Find(x =>
                    {
                        PowerEntity tagEntity = x.GetValue(TagProperty) as PowerEntity;

                        if (tagEntity.Id == hitLine.FirstEnd)
                        {
                            return true;
                        }

                        return false;
                    });

                    GeometryModel3D secondEndModel = _entityModelsList.Find(x =>
                    {
                        PowerEntity tagEntity = x.GetValue(TagProperty) as PowerEntity;

                        if (tagEntity.Id == hitLine.SecondEnd)
                        {
                            return true;
                        }

                        return false;
                    });

                    firstEndModel.Material = new DiffuseMaterial(Brushes.Gold);
                    secondEndModel.Material = new DiffuseMaterial(Brushes.Gold);

                    _lineSegmentModel = hitModel as GeometryModel3D;
                    _firstEndModel = firstEndModel;
                    _secondEndModel = secondEndModel;
                }
            }

            return HitTestResultBehavior.Stop;
        }

        private void ResetLineEndModel(GeometryModel3D lineEnd)
        {
            PowerEntity tagEntity = _firstEndModel?.GetValue(TagProperty) as PowerEntity;

            if (tagEntity == null)
            {
                return;
            }
            else if (tagEntity is SubstationEntity)
            {
                lineEnd.Material = new DiffuseMaterial(Brushes.Red);
            }
            else if (tagEntity is NodeEntity)
            {
                lineEnd.Material = new DiffuseMaterial(Brushes.Green);
            }
            else if (tagEntity is SwitchEntity)
            {
                lineEnd.Material = new DiffuseMaterial(Brushes.Blue);
            }
        }

        private Point CreatePoint(double entityX, double entityY)
        {
            double pointX = Math.Round((entityX - _minLongitude) / (_maxLongitude - _minLongitude) * 235 - 117.5);
            double pointY = Math.Round((entityY - _minLatitude) / (_maxLatitude - _minLatitude) * 155 - 77.5);

            return new Point(pointX, pointY)
            {
                X = RoundToNearestMultiple(pointX, 1.5),
                Y = RoundToNearestMultiple(pointY, 1.5)
            };
        }

        private double RoundToNearestMultiple(double value, double multiple)
        {
            return (double)Math.Round((decimal)value / (decimal)multiple, MidpointRounding.AwayFromZero) * multiple;
        }

        private bool IsOnMap(double latitude, double longitude)
        {
            if (latitude < _minLatitude || latitude > _maxLatitude)
            {
                return false;
            }

            if (longitude < _minLongitude || longitude > _maxLongitude)
            {
                return false;
            }

            return true;
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
