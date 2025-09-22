using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LiveCharts;
using LiveCharts.Wpf;

namespace AuroraDashboard
{
    public class MineralPriceMod
    {
        public double[] priceMult = new double[Enum.GetValues(typeof(MineralType)).Length];

        public static MineralPriceMod noMod;
        public static MineralPriceMod usefulnessMod;

        static MineralPriceMod()
        {
            noMod = new MineralPriceMod();
            usefulnessMod = new MineralPriceMod();

            for (int i = 0; i < noMod.priceMult.Length; i++)
            {
                noMod.priceMult[i] = 1;
                usefulnessMod.priceMult[i] = 1;
            }

            usefulnessMod.priceMult[(int)MineralType.Duranium] = 1.1;
            usefulnessMod.priceMult[(int)MineralType.Vendarite] = 0.7;
            usefulnessMod.priceMult[(int)MineralType.Corbomite] = 0.8;
            usefulnessMod.priceMult[(int)MineralType.Corundium] = 1.2;
            usefulnessMod.priceMult[(int)MineralType.Gallicite] = 1.6;
        }
    }

    class ProspectListItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string[] MineralValues { private set; get; }

        public ProspectListItem()
        {
            MineralValues = new string[Enum.GetValues(typeof(MineralType)).Length];
        }
    }

    class WeaponListItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged("IsSelected"); } }
        public string Name { get; set; }
        public string Size { set; get; }
        public string Cost { set; get; }
        public string Price { set; get; }

        public AurCompWeapon weapon;
        public AurClass cls;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AuroraData aurData;

        AurGame curGame;
        AurRace curRace;

        public double[] mineralPrices = new double[Enum.GetValues(typeof(MineralType)).Length];
        public double[] mineralProduction = new double[Enum.GetValues(typeof(MineralType)).Length];

        public double GetCombinedMineralPrice(double[] minerals)
        {
            double ret = 0;
            for (int i = 0; i < minerals.Length; i++)
            {
                ret += minerals[i] * mineralPrices[i];
            }
            return ret;
        }

        public Func<double, string> mineralLabelFunc = (value) => (value / 1000).ToString("N0") + "K";

        public static double getDmgAtDist(double baseDmg, double rangeMod, double maxRange, double range)
        {
            if (range <= maxRange)
            {
                double rangeFalloff = 1;

                if (range > rangeMod)
                {
                    rangeFalloff = rangeMod / range;
                }

                return Math.Floor(baseDmg * rangeFalloff);
            }

            return 0;
        }

        private void CalcMineralPrice()
        {
            MineralPriceMod priceMod = MineralPriceMod.noMod;
            switch (cmbPriceMod.SelectedIndex)
            {
                case 1:
                    priceMod = MineralPriceMod.usefulnessMod;
                    break;
            }

            double projectionYears;
            if (!double.TryParse(txtPriceProj.Text, out projectionYears))
            {
                projectionYears = 5.0;
            }

            double[] mineralProjection = new double[curRace.minerals.Length];
            for (int i = 0; i < curRace.minerals.Length; i++)
            {
                mineralProjection[i] = curRace.minerals[i] + mineralProduction[i] * projectionYears;
            }

            double mineralSum = 0;
            for (int i = 0; i < curRace.minerals.Length; i++)
            {
                mineralSum += mineralProjection[i] / priceMod.priceMult[i];
            }

            double avgMineralAmt = mineralSum / curRace.minerals.Length;

            for (int i = 0; i < curRace.minerals.Length; i++)
            {
                mineralPrices[i] = avgMineralAmt / (mineralProjection[i] / priceMod.priceMult[i]);
            }

            OnPriceChanged();
        }

        public SeriesCollection piePopulationSeries = new SeriesCollection();
        public SeriesCollection pieStockpilesSeries = new SeriesCollection();
        public SeriesCollection pieMiningSeries = new SeriesCollection();
        public GridView prospectGrid;

        private double _zoom = 1.0;
        private Point _offset = new Point();
        private bool _mapInitialized = false;
        private bool _isPanning = false;
        private Point _panStartPoint;

        void InitMineralSelector(ComboBox comboBox)
        {
            comboBox.Items.Add("All minerals (amount)");
            comboBox.Items.Add("All minerals (price)");
            foreach (string name in Enum.GetNames(typeof(MineralType)))
            {
                comboBox.Items.Add(name);
            }
            comboBox.SelectedIndex = 0;
        }

        Func<double[], double> GetMineralSelectorFunc(ComboBox comboBox)
        {
            Func<double[], double> amountFunc;
            if (comboBox.SelectedIndex > 1)
            {
                amountFunc = minerals => minerals[comboBox.SelectedIndex - 2];
            }
            else if (comboBox.SelectedIndex == 1)
            {
                amountFunc = minerals =>
                {
                    return GetCombinedMineralPrice(minerals);
                };
            }
            else
            {
                amountFunc = minerals => minerals.Sum();
            }
            return amountFunc;
        }

        readonly int pieStockpilesLimit = 20;
        readonly int pieMiningLimit = 15;
        readonly int chrtMiningLimit = 5;

        public MainWindow()
        {
            InitializeComponent();

            InitMineralSelector(cmbStockpilePieMineral);
            InitMineralSelector(cmbProspectMineral);

            cmbProspectTarget.Items.Add("All bodies");
            cmbProspectTarget.Items.Add("Only colonized");
            cmbProspectTarget.Items.Add("Only uncolonized");
            cmbProspectTarget.SelectedIndex = 0;

            cmbProspectFor.Items.Add("Productivity");
            cmbProspectFor.Items.Add("Productivity * Amount");
            cmbProspectFor.Items.Add("Amount");
            cmbProspectFor.SelectedIndex = 1;

            piePopulation.Series = piePopulationSeries;
            pieStockpiles.Series = pieStockpilesSeries;
            pieMining.Series = pieMiningSeries;

            chrtMineralPrice.Series[0].LabelPoint = (chartPoint) => chartPoint.Y.ToString("F2");
            chrtMineralPrice.AxisX[0].Labels = Enum.GetNames(typeof(MineralType));
            chrtMineralPrice.AxisY[0].MinValue = 0;
            chrtMineralPrice.AxisY[0].MaxValue = 4;

            chrtMineralStockpile.Series[0].LabelPoint = (chartPoint) => mineralLabelFunc(chartPoint.Y);
            chrtMineralStockpile.AxisX[0].Labels = Enum.GetNames(typeof(MineralType));

            chrtMineralMining.AxisX[0].Labels = Enum.GetNames(typeof(MineralType));

            chrtProspect.Series[0].LabelPoint = (chartPoint) =>
            {
                if (cmbProspectFor.SelectedIndex == 0)
                {
                    return chrtProspect.AxisY[0].Labels[(int)chartPoint.Y] + "\n" + chartPoint.X.ToString("F2");
                }
                else
                {
                    return chrtProspect.AxisY[0].Labels[(int)chartPoint.Y] + "\n" + mineralLabelFunc(chartPoint.X);
                }
            };

            prospectGrid = (GridView)lstProspect.View;
            prospectGrid.Columns.Add(new GridViewColumn() { Header = "Name", Width = 150, DisplayMemberBinding = new Binding("Name") });
            prospectGrid.Columns.Add(new GridViewColumn() { Header = "Value", Width = 70, DisplayMemberBinding = new Binding("Value") });

            for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++)
            {
                prospectGrid.Columns.Add(new GridViewColumn() { Header = Enum.GetName(typeof(MineralType), i), Width = 103, DisplayMemberBinding = new Binding("MineralValues[" + i + "]") });
            }

            GalacticMapCanvas.MouseWheel += GalacticMapCanvas_MouseWheel;
            GalacticMapCanvas.MouseDown += GalacticMapCanvas_MouseDown;
            GalacticMapCanvas.MouseMove += GalacticMapCanvas_MouseMove;
            GalacticMapCanvas.MouseUp += GalacticMapCanvas_MouseUp;
        }

        private void GalacticMapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(GalacticMapCanvas);
                GalacticMapCanvas.CaptureMouse();
            }
        }

        private void GalacticMapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPoint = e.GetPosition(GalacticMapCanvas);
                _offset.X += currentPoint.X - _panStartPoint.X;
                _offset.Y += currentPoint.Y - _panStartPoint.Y;
                _panStartPoint = currentPoint;
                DrawGalacticMap();
            }
        }

        private void GalacticMapCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                GalacticMapCanvas.ReleaseMouseCapture();
            }
        }

        private void GalacticMapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(GalacticMapCanvas);
            double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1;

            _zoom *= zoomFactor;

            _offset.X = mousePos.X - (mousePos.X - _offset.X) * zoomFactor;
            _offset.Y = mousePos.Y - (mousePos.Y - _offset.Y) * zoomFactor;

            DrawGalacticMap();
        }

        private async void btnLoadDB_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Aurora Database|*.db|All Files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                Progress<AuroraDBReader.PrgMsg> progress = new Progress<AuroraDBReader.PrgMsg>();
                progress.ProgressChanged += UpdateProgress;

                txtOutput.Visibility = Visibility.Visible;

                AuroraDBReader dbReader = new AuroraDBReader();
                await Task.Run(() =>
                {
                    aurData = dbReader.ReadDB(openFileDialog.FileName, progress);
                });

                if (aurData != null)
                {
                    txtOutput.Visibility = Visibility.Hidden;
                    ReloadGameCmb();
                }
                else
                {
                    txtOutput.Text += "Error loading database file.\n";
                }
            }
        }

        private void UpdateProgress(object sender, AuroraDBReader.PrgMsg prg)
        {
            prgLoad.Value = prg.progress * prgLoad.Maximum;
            txtOutput.Text = txtOutput.Text + "[" + (prg.progress * 100).ToString("000.0") + "%] " + prg.msg + "\n";
        }

        void ReloadGameCmb()
        {
            cmbGame.Items.Clear();
            foreach (AurGame game in aurData.Games)
            {
                cmbGame.Items.Add(game.name);
            }

            cmbGame.SelectedIndex = 1;
        }

        void ReloadRaceCmb()
        {
            cmbRace.Items.Clear();
            if (curGame == null)
            {
                return;
            }

            foreach (AurRace race in curGame.Races)
            {
                cmbRace.Items.Add(race.name);
            }

            cmbRace.SelectedIndex = 0;
        }

        void OnPriceChanged()
        {
            chrtMineralPrice.Series[0].Values = new ChartValues<double>(mineralPrices);

            if (cmbStockpilePieMineral.SelectedIndex == 1)
            {
                RecalculateStockpilePie();
            }

            if (cmbProspectMineral.SelectedIndex == 1)
            {
                UpdateProspect();
            }

            if (chkAnalyzePerPrice.IsChecked == true)
            {
                UpdateWeaponAnalysis();
            }
        }

        void OnRaceSelected()
        {
            MineralPriceMod priceMod = MineralPriceMod.noMod;

            // Also calculates data for production amounts that figures in price
            RecalculateMiningCharts();

            CalcMineralPrice();

            RecalculateStockpilePie();
            chrtMineralStockpile.Series[0].Values = new ChartValues<double>(curRace.minerals);

            UpdateProspect();

            PopulateWeaponList();

            PopulatePopSummary();

            _mapInitialized = false;
        }

        private void InitGalacticMap()
        {
            // Find the bounding box of the systems
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            foreach (var system in curRace.knownSystems)
            {
                if (system.x < minX) minX = system.x;
                if (system.x > maxX) maxX = system.x;
                if (system.y < minY) minY = system.y;
                if (system.y > maxY) maxY = system.y;
            }

            double rangeX = maxX - minX;
            double rangeY = maxY - minY;

            if (rangeX == 0) rangeX = 1;
            if (rangeY == 0) rangeY = 1;

            double scaleX = GalacticMapCanvas.ActualWidth / rangeX;
            double scaleY = GalacticMapCanvas.ActualHeight / rangeY;
            _zoom = Math.Min(scaleX, scaleY) * 0.9;

            _offset.X = (GalacticMapCanvas.ActualWidth - rangeX * _zoom) / 2 - minX * _zoom;
            _offset.Y = (GalacticMapCanvas.ActualHeight - rangeY * _zoom) / 2 - minY * _zoom;
        }

        private void DrawGalacticMap()
        {
            GalacticMapCanvas.Children.Clear();

            if (curRace == null)
            {
                return;
            }

            foreach (var system in curRace.knownSystems)
            {
                var ellipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.White,
                    ToolTip = system.Name
                };

                var textBlock = new TextBlock
                {
                    Text = system.Name,
                    Foreground = Brushes.White,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
                grid.Children.Add(ellipse);
                grid.Children.Add(textBlock);
                Grid.SetRow(ellipse, 0);
                Grid.SetRow(textBlock, 1);

                double canvasX = system.x * _zoom + _offset.X;
                double canvasY = system.y * _zoom + _offset.Y;

                Canvas.SetLeft(grid, canvasX - 2.5);
                Canvas.SetTop(grid, canvasY - 2.5);

                GalacticMapCanvas.Children.Add(grid);
            }
        }

        void PopulateWeaponList()
        {
            lstWeapons.Items.Clear();

            cmbWeapFC.Items.Clear();
            foreach (var bfc in curRace.knownCompIdx[ComponentType.BFC])
            {
                cmbWeapFC.Items.Add(bfc.component.name);
            }

            if (optAnalyzeWeapon.IsChecked == true)
            {
                foreach (var weaponComp in curRace.knownCompIdx[ComponentType.Weapon])
                {
                    if (chkAnalyzeObsolete.IsChecked == true || !weaponComp.isObsolete)
                    {
                        WeaponListItem item = new WeaponListItem();
                        AurCompWeapon weapon = (AurCompWeapon)weaponComp.component;
                        item.Name = weapon.name;
                        item.Size = weapon.size.ToString("N0");
                        item.weapon = weapon;
                        item.Cost = weapon.cost.ToString("N0");
                        item.Price = GetCombinedMineralPrice(weapon.mineralCost).ToString("N0");
                        item.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                        {
                            UpdateWeaponAnalysis();
                        };

                        lstWeapons.Items.Add(item);
                    }
                }
            }
            else
            {
                foreach (AurClass cls in curRace.classes)
                {
                    if (cls.ppv > 0 && (chkAnalyzeObsolete.IsChecked == true || !cls.isObsolete))
                    {
                        WeaponListItem item = new WeaponListItem();
                        item.Name = cls.name;
                        item.Size = (cls.tonnage / 50).ToString("N0");
                        item.cls = cls;
                        item.Cost = cls.cost.ToString("N0");
                        item.Price = GetCombinedMineralPrice(cls.mineralCost).ToString("N0");
                        item.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                        {
                            UpdateWeaponAnalysis();
                        };

                        lstWeapons.Items.Add(item);
                    }
                }
            }

            UpdateWeaponAnalysis();
        }

        double weaponFCRange = 400000;
        double weaponFCSpeed = 4000;
        double weaponShipSpeed = 4000;
        double weaponTargetSpeed = 4000;
        Dictionary<AurCompWeapon, LineSeries> analyzedWeapons = new Dictionary<AurCompWeapon, LineSeries>();
        Dictionary<AurClass, LineSeries> analyzedClasses = new Dictionary<AurClass, LineSeries>();
        void UpdateWeaponAnalysis()
        {
            chrtWeapons.UpdaterState = UpdaterState.Paused;

            double maxRange = 0;
            if (optAnalyzeWeapon.IsChecked == true)
            {
                if (analyzedClasses.Keys.Count > 0)
                {
                    chrtWeapons.Series.Clear();
                }
                analyzedClasses.Clear();

                foreach (WeaponListItem item in lstWeapons.Items)
                {
                    if (item.IsSelected)
                    {
                        if (!analyzedWeapons.ContainsKey(item.weapon))
                        {
                            LineSeries series = new LineSeries();

                            chrtWeapons.Series.Add(series);
                            analyzedWeapons.Add(item.weapon, series);
                        }
                    }
                    else
                    {
                        if (analyzedWeapons.ContainsKey(item.weapon))
                        {
                            chrtWeapons.Series.Remove(analyzedWeapons[item.weapon]);
                            analyzedWeapons.Remove(item.weapon);
                        }
                    }
                }
            }
            else
            {
                if (analyzedWeapons.Keys.Count > 0)
                {
                    chrtWeapons.Series.Clear();
                }
                analyzedWeapons.Clear();

                foreach (WeaponListItem item in lstWeapons.Items)
                {
                    if (item.IsSelected)
                    {
                        if (!analyzedClasses.ContainsKey(item.cls))
                        {
                            LineSeries series = new LineSeries();

                            chrtWeapons.Series.Add(series);
                            analyzedClasses.Add(item.cls, series);
                        }
                    }
                    else
                    {
                        if (analyzedClasses.ContainsKey(item.cls))
                        {
                            chrtWeapons.Series.Remove(analyzedClasses[item.cls]);
                            analyzedClasses.Remove(item.cls);
                        }
                    }
                }
            }

            double maxWeaponFCRange = weaponFCRange;
            if (optAnalyzeWeapon.IsChecked == true)
            {
                foreach (AurCompWeapon weapon in analyzedWeapons.Keys)
                {
                    maxRange = Math.Max(weapon.rangeMax, maxRange);
                }
            }
            else
            {
                foreach (AurClass cls in analyzedClasses.Keys)
                {
                    foreach (AurClass.Comp bfc in cls.components[ComponentType.BFC])
                    {
                        maxWeaponFCRange = Math.Max(((AurCompBFC)bfc.component).rangeMax, maxWeaponFCRange);
                    }

                    foreach (AurClass.Comp weaponType in cls.components[ComponentType.Weapon])
                    {
                        maxRange = Math.Max(((AurCompWeapon)weaponType.component).rangeMax, maxRange);
                    }
                }
            }

            maxRange = Math.Min(maxRange, maxWeaponFCRange);

            int sampleCnt = 100;
            List<string> labelsX = new List<string>();
            for (int i = 0; i < sampleCnt; i++)
            {
                double dist = maxRange * i / sampleCnt;
                labelsX.Add(dist.ToString("N0"));
            }
            //List<string> labelsY = new List<string>();

            if (optAnalyzeWeapon.IsChecked == true)
            {
                foreach (AurCompWeapon weapon in analyzedWeapons.Keys)
                {
                    analyzedWeapons[weapon].Values = new ChartValues<double>();
                    analyzedWeapons[weapon].Title = weapon.name;
                    analyzedWeapons[weapon].LineSmoothness = 0;

                    int shotInterval = weapon.powerRequired > 0 ? ((int)Math.Ceiling(weapon.powerRequired / weapon.recharge)) : 1;

                    double trackingSpeed = weapon.trackingSpeed > 0 ? weapon.trackingSpeed : weaponShipSpeed;
                    trackingSpeed = Math.Min(trackingSpeed, weaponFCSpeed);

                    for (int i = 0; i < sampleCnt; i++)
                    {
                        double dist = maxRange * i / sampleCnt;

                        double dmg = GetWeaponAnalysisValue(weapon, dist, weaponFCRange, shotInterval, trackingSpeed, weaponTargetSpeed, weapon.size, weapon.cost, weapon.mineralCost);

                        if (dmg > 0)
                        {
                            analyzedWeapons[weapon].Values.Add(dmg);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (AurClass cls in analyzedClasses.Keys)
                {
                    analyzedClasses[cls].Values = new ChartValues<double>();
                    analyzedClasses[cls].Title = cls.name;
                    analyzedClasses[cls].LineSmoothness = 0;

                    for (int i = 0; i < sampleCnt; i++)
                    {
                        double dist = maxRange * i / sampleCnt;
                        double dmgSum = 0;

                        foreach (AurClass.Comp weaponType in cls.components[ComponentType.Weapon])
                        {
                            AurCompWeapon weapon = (AurCompWeapon)weaponType.component;

                            int shotInterval = weapon.powerRequired > 0 ? ((int)Math.Ceiling(weapon.powerRequired / weapon.recharge)) : 1;
                            double weaponTrackingSpeed = weapon.trackingSpeed > 0 ? weapon.trackingSpeed : cls.maxSpeed;

                            double dmgPotential = 0;
                            foreach (AurClass.Comp bfc in cls.components[ComponentType.BFC])
                            {
                                double fcRange = ((AurCompBFC)bfc.component).rangeMax;
                                double fcTrackingSpeed = ((AurCompBFC)bfc.component).trackingSpeed;

                                double trackingSpeed = Math.Min(weaponTrackingSpeed, fcTrackingSpeed);

                                double dmg = GetWeaponAnalysisValue(weapon, dist, fcRange, shotInterval, trackingSpeed, weaponTargetSpeed, cls.tonnage / 50, cls.cost, cls.mineralCost);

                                dmgPotential = Math.Max(dmg * weaponType.number, dmgPotential);
                            }

                            dmgSum += dmgPotential;
                        }


                        if (dmgSum > 0)
                        {
                            analyzedClasses[cls].Values.Add(dmgSum);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            chrtWeapons.AxisX[0].Labels = labelsX;
            chrtWeapons.UpdaterState = UpdaterState.Running;
            chrtWeapons.Update();
        }

        double GetWeaponAnalysisValue(AurCompWeapon weapon, double dist, double fcRange, int shotInterval, double fcTrackingSpeed, double targetSpeed, double hs, double cost, double[] mineralCost)
        {
            double dmg = getDmgAtDist(weapon.damage, weapon.rangeMod, weapon.rangeMax, dist) * weapon.shots;

            if (optAnalyzeShots.IsChecked == true)
            {
                if (dmg > 0)
                {
                    dmg = weapon.shots;
                }
                else
                {
                    dmg = 0;
                }
            }

            dmg *= weapon.toHitMod;

            if (chkAnalyzePer5s.IsChecked == true)
            {
                dmg /= shotInterval;
            }

            if (chkAnalyzePerHS.IsChecked == true)
            {
                dmg /= hs;
            }
            else if (chkAnalyzePerCost.IsChecked == true)
            {
                dmg /= cost;
            }
            else if (chkAnalyzePerPrice.IsChecked == true)
            {
                double price = GetCombinedMineralPrice(mineralCost);

                dmg /= price;
            }

            if (chkAnalyzeWithFC.IsChecked == true)
            {
                dmg *= (1.0 - dist / fcRange);

                if (fcTrackingSpeed < targetSpeed)
                {
                    dmg *= (fcTrackingSpeed / targetSpeed);
                }
            }

            return dmg;
        }

        double prospectAmountCap = 10000000;
        double prospectMinAmount = 10000;
        int prospectResCnt = 50;
        int prospectChrtCnt = 15;

        void UpdateProspect()
        {
            chrtProspect.UpdaterState = UpdaterState.Paused;

            bool filterFunc(AurBody body)
            {
                if (cmbProspectTarget.SelectedIndex != 0)
                {
                    return (body.populations.Count > 0) == (cmbProspectTarget.SelectedIndex == 1);
                }

                return true;
            }

            Func<double[], double> amountFunc = GetMineralSelectorFunc(cmbProspectMineral);
            double valueFunc(AurBody body)
            {
                double[] mineralsAdjsuted = new double[Enum.GetValues(typeof(MineralType)).Length];
                for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++)
                {
                    switch (cmbProspectFor.SelectedIndex)
                    {
                        case 0:
                            mineralsAdjsuted[i] = body.minerals[i] >= prospectMinAmount ? body.mineralsAcc[i] : 0;
                            break;
                        case 1:
                            mineralsAdjsuted[i] = Math.Min(body.minerals[i], prospectAmountCap) * body.mineralsAcc[i];
                            break;
                        case 2:
                            mineralsAdjsuted[i] = Math.Min(body.minerals[i], prospectAmountCap);
                            break;
                    }
                }

                return amountFunc(mineralsAdjsuted);
            }

            ChartValues<double> values = new ChartValues<double>();
            List<string> labels = new List<string>();
            chrtProspect.Series[0].Values = values;
            chrtProspect.AxisY[0].Labels = labels;
            lstProspect.Items.Clear();

            int cnt = 0;
            foreach (AurBody body in (from dictRec in curGame.bodyIdx where (dictRec.Value.hasMinerals && dictRec.Value.isSurveyed[curRace] && filterFunc(dictRec.Value)) orderby valueFunc(dictRec.Value) descending select dictRec.Value).Take(prospectResCnt))
            {
                double value = valueFunc(body);
                string name = body.GetName(curRace);

                if (cnt < prospectChrtCnt)
                {
                    values.Add(value);
                    labels.Add(name);
                }

                ProspectListItem listItem = new ProspectListItem();
                listItem.Name = name;
                listItem.Value = value > 1000 ? (value / 1000).ToString("N0") + "K" : value.ToString("F2");

                for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++)
                {
                    listItem.MineralValues[i] = body.minerals[i].ToString("N0") + " (" + body.mineralsAcc[i].ToString("N1") + ")";
                }

                lstProspect.Items.Add(new ListViewItem() { Content = listItem, HorizontalContentAlignment = HorizontalAlignment.Right });
                cnt++;
            }

            chrtProspect.UpdaterState = UpdaterState.Running;
            chrtProspect.Update();
        }

        void PopulatePopSummary()
        {
            RecalculatePopPie();

            RecalculateEmpireSummary();
        }

        public class EmpireSummaryEntry
        {
            public string Description { get; set; }
            public string Value { get; set; }
            public string Category { get; set; }
        }

        void RecalculateEmpireSummary()
        {
            List<EmpireSummaryEntry> EmpireSummaryList = new List<EmpireSummaryEntry>();

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Overall population (M)",
                Value = curRace.populationSum.ToString("N0"),
                Category = "Population"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Number of populated colonies",
                Value = (from pop in curRace.populations where pop.population > 0 select pop).Count().ToString("N0"),
                Category = "Population"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Number of colonies",
                Value = curRace.populations.Count.ToString("N0"),
                Category = "Population"
            });

            for (int i = 0; i < curRace.installations.Length; i++)
            {
                EmpireSummaryList.Add(new EmpireSummaryEntry()
                {
                    Description = Enum.GetName(typeof(InstallationType), i),
                    Value = curRace.installations[i].ToString("N0"),
                    Category = "Installations"
                });
            }

            double orbitalFuelProd = 0;
            foreach (AurFleet fleet in curRace.fleets)
            {
                if (fleet.orbitBody != null && fleet.orbitBody.minerals[(int)MineralType.Sorium] > 0)
                {
                    int harvesters = 0;
                    foreach (AurShip ship in fleet.ships)
                    {
                        harvesters += ship.shipClass.fuelHarvestingModules;
                    }

                    orbitalFuelProd += harvesters * curRace.prodFuel * fleet.orbitBody.mineralsAcc[(int)MineralType.Sorium];
                }
            }

            double planetFuelProd = 0;
            foreach (AurPop pop in curRace.populations)
            {
                if (pop.fuelProdEnabled)
                {
                    planetFuelProd += pop.installations[(int)InstallationType.Refinery] * curRace.prodFuel * pop.prodEfficiency;
                }
            }

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Fuel production (L/year)",
                Value = (planetFuelProd + orbitalFuelProd).ToString("N0"),
                Category = "Fuel"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Fuel production (orbital) (L/year)",
                Value = (orbitalFuelProd).ToString("N0"),
                Category = "Fuel"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Fuel at populations (L)",
                Value = curRace.popFuelSum.ToString("N0"),
                Category = "Fuel"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Fuel on ships (L)",
                Value = curRace.shipFuelSum.ToString("N0"),
                Category = "Fuel"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Ship fuel capacity (L)",
                Value = curRace.shipFuelCapSum.ToString("N0"),
                Category = "Fuel"
            });

            double planetMSPProd = 0;
            foreach (AurPop pop in curRace.populations)
            {
                if (pop.mspProdEnabled)
                {
                    planetMSPProd += pop.installations[(int)InstallationType.MaintenanceFacility] * curRace.prodMSP * 4 * pop.prodEfficiency;
                }
            }

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = " Annual ship MSP cost",
                Value = curRace.shipMSPAnnualCost.ToString("N0"),
                Category = "Maintenance"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Annual MSP production",
                Value = planetMSPProd.ToString("N0"),
                Category = "Maintenance"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "MSP at populations",
                Value = curRace.popMSPSum.ToString("N0"),
                Category = "Maintenance"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "MSP on ships",
                Value = curRace.shipMSPSum.ToString("N0"),
                Category = "Maintenance"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Ship MSP capacity",
                Value = curRace.shipMSPCapSum.ToString("N0"),
                Category = "Maintenance"
            });

            double milTonSum = 0, milShipTonSum = 0, comTonSum = 0, comShipTonSum = 0, civTonSum = 0, fighterCount = 0, fighterTonnage = 0, milShipCount = 0, comShipCount = 0, civShipCount = 0;

            double cargoCap = 0;
            double cargoThroughput = 0;
            double cargoThroughputNonciv = 0;
            double colonistCap = 0;
            double colonistThroughput = 0;
            double colonistThroughputNonciv = 0;
            double tankerCap = 0;
            double tankerThroughput = 0;
            foreach (AurShip ship in curRace.ships)
            {
                if (ship.isCivilianLine)
                {
                    civTonSum += ship.shipClass.tonnage;
                    civShipCount++;
                }
                else if (ship.shipClass.isMilitary)
                {
                    milTonSum += ship.shipClass.tonnage;
                    milShipCount++;
                    if (ship.shipClass.maxSpeed > 1)
                    {
                        milShipTonSum += ship.shipClass.tonnage;
                    }
                }
                else
                {
                    comTonSum += ship.shipClass.tonnage;
                    comShipCount++;

                    if (ship.shipClass.maxSpeed > 1)
                    {
                        comShipTonSum += ship.shipClass.tonnage;
                    }
                }

                if (ship.shipClass.isFighter)
                {
                    fighterCount++;
                    fighterTonnage += ship.shipClass.tonnage;
                }

                if (ship.shipClass.cargoCapacity > 0)
                {
                    cargoCap += ship.shipClass.cargoCapacity;
                    cargoThroughput += ship.shipClass.cargoCapacity * ship.shipClass.maxSpeed;

                    if (!ship.isCivilianLine)
                    {
                        cargoThroughputNonciv += ship.shipClass.cargoCapacity * ship.shipClass.maxSpeed;
                    }
                }

                if (!ship.shipClass.isMilitary && ship.shipClass.colonistCapacity > 0)
                {
                    colonistCap += ship.shipClass.colonistCapacity;
                    colonistThroughput += ship.shipClass.colonistCapacity * ship.shipClass.maxSpeed;

                    if (!ship.isCivilianLine)
                    {
                        colonistThroughputNonciv += ship.shipClass.colonistCapacity * ship.shipClass.maxSpeed;
                    }
                }

                if (ship.shipClass.isTanker)
                {
                    tankerCap += ship.shipClass.fuel;
                    tankerThroughput += ship.shipClass.fuel * ship.shipClass.maxSpeed;
                }
            }

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Cargo ship capacity (CargoUnits)",
                Value = cargoCap.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Colonist ship capacity (Colonists)",
                Value = colonistCap.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Tanker ship capacity (L)",
                Value = tankerCap.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Cargo ship throughput (CargoUnits * km/s)",
                Value = cargoThroughput.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Cargo ship throughput (CargoUnits * km/s) (Non-civilian)",
                Value = cargoThroughputNonciv.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Colonist ship throughput (Colonists * km/s)",
                Value = colonistThroughput.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Colonist ship throughput (Colonists * km/s) (Non-civilian)",
                Value = colonistThroughputNonciv.ToString("N0"),
                Category = "Logistics"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Tanker ship throughput (L * km/s)",
                Value = tankerThroughput.ToString("N0"),
                Category = "Logistics"
            });

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Military Tonnage",
                Value = milTonSum.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Military Tonnage (Ships)",
                Value = milShipTonSum.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Commercial Tonnage",
                Value = comTonSum.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Commercial Tonnage (Ships)",
                Value = comShipTonSum.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Civilian Tonnage",
                Value = civTonSum.ToString("N0"),
                Category = "Fleets"
            });

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Military Ships",
                Value = milShipCount.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Commercial Ships",
                Value = comShipCount.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Civilian Ships",
                Value = civShipCount.ToString("N0"),
                Category = "Fleets"
            });

            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Fighter Count",
                Value = fighterCount.ToString("N0"),
                Category = "Fleets"
            });
            EmpireSummaryList.Add(new EmpireSummaryEntry()
            {
                Description = "Fighter Tonnage",
                Value = fighterTonnage.ToString("N0"),
                Category = "Fleets"
            });

            ListCollectionView collectionView = new ListCollectionView(EmpireSummaryList);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            dgEmpireSummary.ItemsSource = collectionView;
            dgEmpireSummary.Columns[0].Width = 350;
            dgEmpireSummary.Columns[1].Width = 150;
            dgEmpireSummary.Columns[1].CellStyle = Resources["numberCellStyle"] as Style;
        }

        void RecalculatePopPie()
        {
            piePopulation.UpdaterState = UpdaterState.Paused;
            piePopulationSeries.Clear();

            Dictionary<AurSpecies, double> speciesPop = new Dictionary<AurSpecies, double>();

            foreach (AurPop pop in curRace.populations)
            {
                if (!speciesPop.ContainsKey(pop.species))
                {
                    speciesPop.Add(pop.species, pop.population);
                }
                else
                {
                    speciesPop[pop.species] += pop.population;
                }
            }

            foreach (AurSpecies species in (from spec in speciesPop.Keys orderby speciesPop[spec] descending select spec))
            {
                bool speciesPieAdded = false;
                foreach (AurPop pop in (from pop in curRace.populations where pop.population > 0 && pop.species == species orderby pop.population descending select pop))
                {
                    PieSeries pieSeries = new PieSeries();
                    piePopulationSeries.Add(pieSeries);

                    ChartValues<double> popVal = new ChartValues<double>();

                    if (!speciesPieAdded)
                    {
                        popVal.Add(speciesPop[species]);
                        speciesPieAdded = true;
                    }
                    else
                    {
                        popVal.Add(0);
                    }

                    popVal.Add(pop.population);

                    pieSeries.Title = "";
                    pieSeries.Values = popVal;
                    pieSeries.LabelPoint = (chartPoint) =>
                    {
                        return chartPoint.X == 0 ? (species.name + ": " + speciesPop[species].ToString("N0") + "M") : pop.name + ": " + pop.population.ToString("N0") + "M";
                    };
                    pieSeries.DataLabels = pop.population / speciesPop[species] > 0.07;
                }
            }

            piePopulation.UpdaterState = UpdaterState.Running;
            piePopulation.Update();
        }

        void RecalculateStockpilePie()
        {
            pieStockpiles.UpdaterState = UpdaterState.Paused;

            Func<double[], double> amountFunc = GetMineralSelectorFunc(cmbStockpilePieMineral);

            int seriesIdx = 0;
            double highestAmount = 0;
            foreach (AurPop pop in (from pop in curRace.populations where amountFunc(pop.minerals) > 0 orderby amountFunc(pop.minerals) descending select pop))
            {
                double amount = amountFunc(pop.minerals);
                highestAmount = Math.Max(highestAmount, amount);

                if (amount / highestAmount < 0.025)
                {
                    break;
                }

                ChartValues<double> val = new ChartValues<double>();
                val.Add(amount);

                PieSeries pieSeries;
                if (pieStockpilesSeries.Count <= seriesIdx)
                {
                    pieSeries = new PieSeries();
                    pieStockpilesSeries.Add(pieSeries);
                }
                else
                {
                    pieSeries = (PieSeries)pieStockpilesSeries[seriesIdx];
                }

                pieSeries.Values = val;
                pieSeries.Title = "";
                pieSeries.LabelPoint = (chartPoint) => { return pop.name + "\n" + mineralLabelFunc(chartPoint.Y); };
                pieSeries.DataLabels = amountFunc(pop.minerals) / amountFunc(curRace.minerals) > 0.07;
                seriesIdx++;
            }

            while (pieStockpilesSeries.Count > seriesIdx)
            {
                pieStockpilesSeries.RemoveAt(seriesIdx);
            }

            pieStockpiles.UpdaterState = UpdaterState.Running;
            pieStockpiles.Update();
        }

        void AddPieMineralsSeries(string name, double mineValue, double highestValue, double[] inst, double omCnt)
        {
            PieSeries pieSeries = new PieSeries();
            pieMiningSeries.Add(pieSeries);

            ChartValues<double> val = new ChartValues<double>();
            val.Add(mineValue);

            pieSeries.Values = val;
            pieSeries.Title = "";
            pieSeries.LabelPoint = (chartPoint) =>
            {
                return name + " (" + mineValue.ToString("N1") + ")\n" +
                    inst[(int)InstallationType.Mine].ToString("N1") + "M " +
                    inst[(int)InstallationType.AutoMine].ToString("N1") + "AM " +
                    inst[(int)InstallationType.CivilianMine].ToString("N1") + "CIVM " +
                    omCnt.ToString() + "OM";
            };
            pieSeries.DataLabels = mineValue / highestValue > 0.33;
        }

        class MineralBar
        {
            public MineralType mineralType;
            public double amount;
        }
        LiveCharts.Configurations.CartesianMapper<MineralBar> mineralBarsMapper = new LiveCharts.Configurations.CartesianMapper<MineralBar>();

        void RecalculateMiningCharts()
        {
            pieMining.UpdaterState = UpdaterState.Paused;
            chrtMineralMining.UpdaterState = UpdaterState.Paused;

            SeriesCollection miningBarSeries = chrtMineralMining.Series;

            double mineValueFunc(AurPop pop)
            {
                int omModuleCnt = 0;
                foreach (AurFleet fleet in pop.oribitingFleets)
                {
                    foreach (AurShip ship in fleet.ships)
                    {
                        omModuleCnt += ship.shipClass.miningModules;
                    }
                }

                double civVal = chkMineIncludeCiv.IsChecked == true ? pop.installations[(int)InstallationType.CivilianMine] * 10 : 0;
                return civVal + pop.installations[(int)InstallationType.Mine] + pop.installations[(int)InstallationType.AutoMine] + omModuleCnt;
            }

            var miningPops = (from pop in curRace.populations where mineValueFunc(pop) > 0 select pop);

            pieMiningSeries.Clear();
            miningBarSeries.Clear();
            double highestValue = 0;
            double othersValSum = 0;
            double[] othersInstSum = new double[Enum.GetValues(typeof(InstallationType)).Length];
            int othersOmCnt = 0;
            int seriesIdx = 0;
            foreach (AurPop pop in (from pop in miningPops orderby mineValueFunc(pop) descending select pop))
            {
                double mineValue = mineValueFunc(pop);
                highestValue = Math.Max(highestValue, mineValue);

                int omModuleCnt = 0;
                foreach (AurFleet fleet in pop.oribitingFleets)
                {
                    foreach (AurShip ship in fleet.ships)
                    {
                        omModuleCnt += ship.shipClass.miningModules;
                    }
                }

                if (seriesIdx < pieMiningLimit && mineValue / highestValue > 0.05)
                {
                    AddPieMineralsSeries(pop.name, mineValue, highestValue, pop.installations, omModuleCnt);
                }
                else
                {
                    othersValSum += mineValue;
                    othersOmCnt += omModuleCnt;
                    for (int i = 0; i < othersInstSum.Length; i++)
                    {
                        othersInstSum[i] += pop.installations[i];
                    }
                }

                seriesIdx++;
            }

            if (othersValSum > 0)
            {
                AddPieMineralsSeries("Others", othersValSum, highestValue, othersInstSum, othersOmCnt);
            }

            mineralBarsMapper.X(v => (int)v.mineralType).Y(v => v.amount);

            double[] othersMiningProdSum = new double[Enum.GetValues(typeof(MineralType)).Length];
            Dictionary<AurPop, StackedColumnSeries> stackedSeries = new Dictionary<AurPop, StackedColumnSeries>();
            for (int i = 0; i < othersMiningProdSum.Length; i++)
            {
                mineralProduction[i] = 0.0;

                seriesIdx = 0;
                foreach (AurPop pop in (from pop in miningPops orderby mineValueFunc(pop) * pop.body.mineralsAcc[i] descending select pop))
                {
                    double mineralProdValue = mineValueFunc(pop) * pop.body.mineralsAcc[i] * curRace.prodMine;

                    mineralProduction[i] += mineralProdValue;

                    if (seriesIdx < chrtMiningLimit)
                    {
                        StackedColumnSeries columnSeries;
                        if (!stackedSeries.ContainsKey(pop))
                        {
                            columnSeries = new StackedColumnSeries(mineralBarsMapper);
                            stackedSeries.Add(pop, columnSeries);

                            columnSeries.Title = pop.name;

                            ChartValues<MineralBar> values = new ChartValues<MineralBar>();
                            columnSeries.Values = values;

                            //columnSeries.LabelPoint = (chartPoint) => pop.name + " " + chartPoint.Y.ToString("F1");
                            columnSeries.DataLabels = true;
                            columnSeries.LabelsPosition = BarLabelPosition.Perpendicular;

                            miningBarSeries.Add(columnSeries);
                        }
                        else
                        {
                            columnSeries = stackedSeries[pop];
                        }

                        columnSeries.Values.Add(new MineralBar() { mineralType = (MineralType)i, amount = mineralProdValue });
                    }
                    else
                    {
                        othersMiningProdSum[i] += mineralProdValue;
                    }

                    seriesIdx++;
                }
            }

            StackedColumnSeries othersColumnSeries = new StackedColumnSeries();
            othersColumnSeries.Title = "Others";
            othersColumnSeries.Values = new ChartValues<double>(othersMiningProdSum);
            miningBarSeries.Add(othersColumnSeries);

            pieMining.UpdaterState = UpdaterState.Running;
            chrtMineralMining.UpdaterState = UpdaterState.Running;
            chrtMineralMining.Update();
            pieMining.Update();
        }

        private void cmbGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGame.SelectedIndex == -1)
            {
                return;
            }
            curGame = aurData.Games[cmbGame.SelectedIndex];

            ReloadRaceCmb();
        }

        private void cmbRace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRace.SelectedIndex == -1)
            {
                return;
            }
            curRace = curGame.Races[cmbRace.SelectedIndex];

            OnRaceSelected();
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            if (txtOutput.Visibility != Visibility.Visible)
            {
                txtOutput.Visibility = Visibility.Visible;
            }
            else
            {
                txtOutput.Visibility = Visibility.Hidden;
            }
        }

        private void cmbPriceMod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curRace != null)
            {
                CalcMineralPrice();
            }
        }

        private void cmbStockpileMineral_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curRace != null)
            {
                RecalculateStockpilePie();
            }
        }

        private void cmbProspect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curRace != null)
            {
                UpdateProspect();
            }
        }

        private void cmbProspectFor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProspectFor.SelectedIndex == 0)
            {
                lblProspectLimit.Content = "Min Amount";
                txtProspectLimit.Text = prospectMinAmount.ToString("F0");
            }
            else
            {
                lblProspectLimit.Content = "Amount Cap";
                txtProspectLimit.Text = prospectAmountCap.ToString("F0");
            }

            cmbProspect_SelectionChanged(sender, e);
        }

        private void txtProspectLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtProspectLimit.Text, out double val))
            {
                if (cmbProspectFor.SelectedIndex == 0)
                {
                    prospectMinAmount = val;
                }
                else
                {
                    prospectAmountCap = val;
                }
            }

            if (curRace != null)
            {
                UpdateProspect();
            }
        }

        private void txtWeapFCRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtWeapFCRange.Text, out double val) && val > 0)
            {
                weaponFCRange = val;
            }

            if (curRace != null)
            {
                UpdateWeaponAnalysis();
            }
        }

        private void txtWeapFCTrack_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtWeapFCTrack.Text, out double val) && val > 0)
            {
                weaponFCSpeed = val;
            }

            if (curRace != null && chkAnalyzeWithFC.IsChecked == true)
            {
                UpdateWeaponAnalysis();
            }
        }

        private void txtWeapShipSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtWeapShipSpeed.Text, out double val) && val > 0)
            {
                weaponShipSpeed = val;
            }

            if (curRace != null && chkAnalyzeWithFC.IsChecked == true)
            {
                UpdateWeaponAnalysis();
            }
        }

        private void txtWeapTargetSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtWeapTargetSpeed.Text, out double val) && val > 0)
            {
                weaponTargetSpeed = val;
            }

            if (curRace != null && chkAnalyzeWithFC.IsChecked == true)
            {
                UpdateWeaponAnalysis();
            }
        }

        private void chkPerCost_Checked(object sender, RoutedEventArgs e)
        {
            if (sender != chkAnalyzePerHS)
            {
                chkAnalyzePerHS.IsChecked = false;
            }
            if (sender != chkAnalyzePerCost)
            {
                chkAnalyzePerCost.IsChecked = false;
            }
            if (sender != chkAnalyzePerPrice)
            {
                chkAnalyzePerPrice.IsChecked = false;
            }
        }

        private void optWeaponAnalysis_Checked(object sender, RoutedEventArgs e)
        {
            if (curRace != null)
            {
                UpdateWeaponAnalysis();
            }
        }

        private void optAnalyzeWeapon_Checked(object sender, RoutedEventArgs e)
        {
            // null check necessary during initialization
            if (cmbWeapFC != null)
            {
                bool analyzeWeapon = optAnalyzeWeapon.IsChecked == true;
                cmbWeapFC.IsEnabled = analyzeWeapon;
                txtWeapFCRange.IsEnabled = analyzeWeapon;
                txtWeapFCTrack.IsEnabled = analyzeWeapon;
                txtWeapShipSpeed.IsEnabled = analyzeWeapon;
            }

            if (curRace != null)
            {
                PopulateWeaponList();
            }
        }

        private void chkAnalyzeObsolete_Click(object sender, RoutedEventArgs e)
        {
            if (curRace != null)
            {
                PopulateWeaponList();
            }
        }

        private void cmbWeapFC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curRace != null && cmbWeapFC.SelectedIndex >= 0)
            {
                AurCompBFC bfc = (AurCompBFC)curRace.knownCompIdx[ComponentType.BFC][cmbWeapFC.SelectedIndex].component;

                txtWeapFCRange.Text = bfc.rangeMax.ToString();
                txtWeapFCTrack.Text = bfc.trackingSpeed.ToString();
            }
        }

        private void chkMineIncludeCiv_Click(object sender, RoutedEventArgs e)
        {
            if (curRace != null)
            {
                RecalculateMiningCharts();
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                piePopulation.UpdaterState = tabPopulations.IsSelected ? UpdaterState.Running : UpdaterState.Paused;

                pieStockpiles.UpdaterState = tabMinerals.IsSelected ? UpdaterState.Running : UpdaterState.Paused;
                pieMining.UpdaterState = tabMinerals.IsSelected ? UpdaterState.Running : UpdaterState.Paused;
                chrtMineralPrice.UpdaterState = tabMinerals.IsSelected ? UpdaterState.Running : UpdaterState.Paused;
                chrtMineralStockpile.UpdaterState = tabMinerals.IsSelected ? UpdaterState.Running : UpdaterState.Paused;
                chrtMineralMining.UpdaterState = tabMinerals.IsSelected ? UpdaterState.Running : UpdaterState.Paused;

                chrtProspect.UpdaterState = tabProspecting.IsSelected ? UpdaterState.Running : UpdaterState.Paused;

                chrtWeapons.UpdaterState = tabWeapons.IsSelected ? UpdaterState.Running : UpdaterState.Paused;

                if (tabGalMap.IsSelected)
                {
                    DrawGalacticMap();
                }
            }
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {

        }

        private void txtPriceProj_TextChanged(object sender, TextChangedEventArgs e)
        {
            double dummy;
            if (double.TryParse(txtPriceProj.Text, out dummy) && curRace != null)
            {
                CalcMineralPrice();
            }
        }
    }
}
