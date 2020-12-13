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

namespace AuroraDashboard {
    public class MineralPriceMod {
        public double[] priceMult = new double[Enum.GetValues(typeof(MineralType)).Length];

        public static MineralPriceMod noMod;
        public static MineralPriceMod usefulnessMod;

        static MineralPriceMod() {
            noMod = new MineralPriceMod();
            usefulnessMod = new MineralPriceMod();

            for (int i = 0; i < noMod.priceMult.Length; i++) {
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

    class ProspectListItem {
        public string Name { get; set; }
        public string Value { get; set; }
        public string[] MineralValues { private set; get; }

        public ProspectListItem() {
            MineralValues = new string[Enum.GetValues(typeof(MineralType)).Length];
        }
    }

    class WeaponListItem : INotifyPropertyChanged {
        private bool _isSelected;
        public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged("IsSelected"); } }
        public string Name { get; set; }
        public string Size { set; get; }
        public string Cost { set; get; }
        public string Price { set; get; }

        public AurCompWeapon weapon;
        public AurClass cls;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
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
        AuroraDBReader dbReader = new AuroraDBReader();

        AurGame curGame;
        AurRace curRace;

        public double[] mineralPrices = new double[Enum.GetValues(typeof(MineralType)).Length];

        public double GetCombinedMineralPrice(double[] minerals) {
            double ret = 0;
            for(int i =0;i < minerals.Length; i++) {
                ret += minerals[i] * mineralPrices[i];
            }
            return ret;
        }

        public Func<double, string> mineralLabelFunc = (value) => (value / 1000).ToString("N0") + "K";

        public static double getDmgAtDist(double baseDmg, double rangeMod, double maxRange, double range) {
            if (range <= maxRange) {
                double rangeFalloff = 1;

                if (range > rangeMod) {
                    rangeFalloff = rangeMod / range;
                }

                return Math.Floor(baseDmg * rangeFalloff);
            }

            return 0;
        }

        private void CalcMineralPrice() {
            MineralPriceMod priceMod = MineralPriceMod.noMod;
            switch (cmbPriceMod.SelectedIndex) {
            case 1:
                priceMod = MineralPriceMod.usefulnessMod;
                break;
            }

            double mineralSum = 0;
            for (int i = 0; i < curRace.minerals.Length; i++) {
                mineralSum += curRace.minerals[i] / priceMod.priceMult[i];
            }

            double avgMineralAmt = mineralSum / curRace.minerals.Length;

            for (int i = 0; i < curRace.minerals.Length; i++) {
                mineralPrices[i] = avgMineralAmt / (curRace.minerals[i] / priceMod.priceMult[i]);
            }

            OnPriceChanged();
        }

        public SeriesCollection pieStockpilesSeries = new SeriesCollection();
        public GridView prospectGrid;

        void InitMineralSelector(ComboBox comboBox) {
            comboBox.Items.Add("All minerals (amount)");
            comboBox.Items.Add("All minerals (price)");
            foreach (string name in Enum.GetNames(typeof(MineralType))) {
                comboBox.Items.Add(name);
            }
            comboBox.SelectedIndex = 0;
        }

        Func<double[], double> GetMineralSelectorFunc(ComboBox comboBox) {
            Func<double[], double> amountFunc;
            if (comboBox.SelectedIndex > 1) {
                amountFunc = minerals => minerals[comboBox.SelectedIndex - 2];
            } else if (comboBox.SelectedIndex == 1) {
                amountFunc = minerals => {
                    return GetCombinedMineralPrice(minerals);
                };
            } else {
                amountFunc = minerals => minerals.Sum();
            }
            return amountFunc;
        }

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

            pieStockpiles.Series = pieStockpilesSeries;

            chrtMineralPrice.Series[0].LabelPoint = (chartPoint) => chartPoint.Y.ToString("F2");
            chrtMineralPrice.AxisX[0].Labels = Enum.GetNames(typeof(MineralType));
            chrtMineralPrice.AxisY[0].MinValue = 0;
            chrtMineralPrice.AxisY[0].MaxValue = 4;

            chrtMineralStockpile.Series[0].LabelPoint = (chartPoint) => mineralLabelFunc(chartPoint.Y);
            chrtMineralStockpile.AxisX[0].Labels = Enum.GetNames(typeof(MineralType));

            chrtProspect.Series[0].LabelPoint = (chartPoint) => {
                if (cmbProspectFor.SelectedIndex == 0) {
                    return chrtProspect.AxisY[0].Labels[(int)chartPoint.Y] + "\n" + chartPoint.X.ToString("F2");
                } else {
                    return chrtProspect.AxisY[0].Labels[(int)chartPoint.Y] + "\n" + mineralLabelFunc(chartPoint.X);
                }
            };

            prospectGrid = (GridView)lstProspect.View;
            prospectGrid.Columns.Add(new GridViewColumn() { Header = "Name", Width = 150, DisplayMemberBinding = new Binding("Name") }); 
            prospectGrid.Columns.Add(new GridViewColumn() { Header = "Value", Width = 70, DisplayMemberBinding = new Binding("Value") });

            for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++) {
                prospectGrid.Columns.Add(new GridViewColumn() { Header = Enum.GetName(typeof(MineralType), i), Width = 103, DisplayMemberBinding = new Binding("MineralValues[" + i  + "]") });
            }
        }

        private async void btnLoadDB_Click(object sender, RoutedEventArgs e) {
            Progress<AuroraDBReader.PrgMsg> progress = new Progress<AuroraDBReader.PrgMsg>();
            progress.ProgressChanged += UpdateProgress;

            txtOutput.Visibility = Visibility.Visible;

            await Task.Run(() => {
                aurData = dbReader.ReadDB(@"Data Source=D:\Projects\AuroraDashboard\AuroraDB.db;", progress);
            });

            txtOutput.Visibility = Visibility.Hidden;
            ReloadGameCmb();
        }

        private void UpdateProgress(object sender, AuroraDBReader.PrgMsg prg) {
            prgLoad.Value = prg.progress * prgLoad.Maximum;
            txtOutput.Text = txtOutput.Text + "[" + (prg.progress * 100).ToString("000.0") + "%] " + prg.msg + "\n";
        }

        void ReloadGameCmb() {
            cmbGame.Items.Clear();
            foreach (AurGame game in aurData.Games) {
                cmbGame.Items.Add(game.name);
            }

            cmbGame.SelectedIndex = 1;
        }

        void ReloadRaceCmb() {
            cmbRace.Items.Clear();
            if (curGame == null) {
                return;
            }

            foreach (AurRace race in curGame.Races) {
                cmbRace.Items.Add(race.name);
            }

            cmbRace.SelectedIndex = 0;
        }

        void OnPriceChanged() {
            chrtMineralPrice.Series[0].Values = new ChartValues<double>(mineralPrices);

            if (cmbStockpilePieMineral.SelectedIndex == 1) {
                RecalculateStockpilePie();
            }

            if (cmbProspectMineral.SelectedIndex == 1) {
                UpdateProspect();
            }

            if (chkAnalyzePerPrice.IsChecked == true) {
                UpdateWeaponAnalysis();
            }
        }

        void OnRaceSelected() {
            MineralPriceMod priceMod = MineralPriceMod.noMod;
            
            CalcMineralPrice();

            PopulateMineralTab();

            UpdateProspect();

            PopulateWeaponList();
        }

        void PopulateMineralTab() {
            RecalculateStockpilePie();

            chrtMineralStockpile.Series[0].Values = new ChartValues<double>(curRace.minerals);
        }

        void PopulateWeaponList() {
            lstWeapons.Items.Clear();

            cmbWeapFC.Items.Clear();
            foreach (var bfc in curRace.knownCompIdx[ComponentType.BFC]) {
                cmbWeapFC.Items.Add(bfc.component.name);
            }

            if (optAnalyzeWeapon.IsChecked == true) {
                foreach (var weaponComp in curRace.knownCompIdx[ComponentType.Weapon]) {
                    if (chkAnalyzeObsolete.IsChecked == true || !weaponComp.isObsolete) {
                        WeaponListItem item = new WeaponListItem();
                        AurCompWeapon weapon = (AurCompWeapon)weaponComp.component;
                        item.Name = weapon.name;
                        item.Size = weapon.size.ToString("N0");
                        item.weapon = weapon;
                        item.Cost = weapon.cost.ToString("N0");
                        item.Price = GetCombinedMineralPrice(weapon.mineralCost).ToString("N0");
                        item.PropertyChanged += (object sender, PropertyChangedEventArgs e) => {
                            UpdateWeaponAnalysis();
                        };

                        lstWeapons.Items.Add(item);
                    }
                }
            } else {
                foreach (AurClass cls in curRace.classes) {
                    if (cls.ppv > 0 && (chkAnalyzeObsolete.IsChecked == true || !cls.isObsolete)) {
                        WeaponListItem item = new WeaponListItem();
                        item.Name = cls.name;
                        item.Size = (cls.tonnage / 50).ToString("N0");
                        item.cls = cls;
                        item.Cost = cls.cost.ToString("N0");
                        item.Price = GetCombinedMineralPrice(cls.mineralCost).ToString("N0");
                        item.PropertyChanged += (object sender, PropertyChangedEventArgs e) => {
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
        void UpdateWeaponAnalysis() {
            chrtWeapons.UpdaterState = UpdaterState.Paused;

            double maxRange = 0;
            if (optAnalyzeWeapon.IsChecked == true) {
                if(analyzedClasses.Keys.Count > 0) {
                    chrtWeapons.Series.Clear();
                }
                analyzedClasses.Clear();

                foreach (WeaponListItem item in lstWeapons.Items) {
                    if (item.IsSelected) {
                        if (!analyzedWeapons.ContainsKey(item.weapon)) {
                            LineSeries series = new LineSeries();

                            chrtWeapons.Series.Add(series);
                            analyzedWeapons.Add(item.weapon, series);
                        }
                    } else {
                        if (analyzedWeapons.ContainsKey(item.weapon)) {
                            chrtWeapons.Series.Remove(analyzedWeapons[item.weapon]);
                            analyzedWeapons.Remove(item.weapon);
                        }
                    }
                }
            } else {
                if (analyzedWeapons.Keys.Count > 0) {
                    chrtWeapons.Series.Clear();
                }
                analyzedWeapons.Clear();

                foreach (WeaponListItem item in lstWeapons.Items) {
                    if (item.IsSelected) {
                        if (!analyzedClasses.ContainsKey(item.cls)) {
                            LineSeries series = new LineSeries();

                            chrtWeapons.Series.Add(series);
                            analyzedClasses.Add(item.cls, series);
                        }
                    } else {
                        if (analyzedClasses.ContainsKey(item.cls)) {
                            chrtWeapons.Series.Remove(analyzedClasses[item.cls]);
                            analyzedClasses.Remove(item.cls);
                        }
                    }
                }
            }

            double maxWeaponFCRange = weaponFCRange;
            if (optAnalyzeWeapon.IsChecked == true) {
                foreach (AurCompWeapon weapon in analyzedWeapons.Keys) {
                    maxRange = Math.Max(weapon.rangeMax, maxRange);
                }
            } else {
                foreach (AurClass cls in analyzedClasses.Keys) {
                    foreach (AurClass.Comp bfc in cls.components[ComponentType.BFC]) {
                        maxWeaponFCRange = Math.Max(((AurCompBFC)bfc.component).rangeMax, maxWeaponFCRange);
                    }

                    foreach (AurClass.Comp weaponType in cls.components[ComponentType.Weapon]) {
                        maxRange = Math.Max(((AurCompWeapon)weaponType.component).rangeMax, maxRange);
                    }
                }
            }

            maxRange = Math.Min(maxRange, maxWeaponFCRange);

            int sampleCnt = 100;
            List<string> labelsX = new List<string>();
            for (int i = 0; i < sampleCnt; i++) {
                double dist = maxRange * i / sampleCnt;
                labelsX.Add(dist.ToString("N0"));
            }
            //List<string> labelsY = new List<string>();

            if (optAnalyzeWeapon.IsChecked == true) {
                foreach (AurCompWeapon weapon in analyzedWeapons.Keys) {
                    analyzedWeapons[weapon].Values = new ChartValues<double>();
                    analyzedWeapons[weapon].Title = weapon.name;
                    analyzedWeapons[weapon].LineSmoothness = 0;

                    int shotInterval = weapon.powerRequired > 0 ? ((int)Math.Ceiling(weapon.powerRequired / weapon.recharge)) : 1;

                    double trackingSpeed = weapon.trackingSpeed > 0 ? weapon.trackingSpeed : weaponShipSpeed;
                    trackingSpeed = Math.Min(trackingSpeed, weaponFCSpeed);

                    for (int i = 0; i < sampleCnt; i++) {
                        double dist = maxRange * i / sampleCnt;

                        double dmg = GetWeaponAnalysisValue(weapon, dist, weaponFCRange, shotInterval, trackingSpeed, weaponTargetSpeed, weapon.size, weapon.cost, weapon.mineralCost);

                        if (dmg > 0) {
                            analyzedWeapons[weapon].Values.Add(dmg);
                        } else {
                            break;
                        }
                    }
                }
            } else {
                foreach (AurClass cls in analyzedClasses.Keys) {
                    analyzedClasses[cls].Values = new ChartValues<double>();
                    analyzedClasses[cls].Title = cls.name;
                    analyzedClasses[cls].LineSmoothness = 0;

                    for (int i = 0; i < sampleCnt; i++) {
                        double dist = maxRange * i / sampleCnt;
                        double dmgSum = 0;

                        foreach (AurClass.Comp weaponType in cls.components[ComponentType.Weapon]) {
                            AurCompWeapon weapon = (AurCompWeapon)weaponType.component;

                            int shotInterval = weapon.powerRequired > 0 ? ((int)Math.Ceiling(weapon.powerRequired / weapon.recharge)) : 1;
                            double weaponTrackingSpeed = weapon.trackingSpeed > 0 ? weapon.trackingSpeed : cls.maxSpeed;

                            double dmgPotential = 0;
                            foreach (AurClass.Comp bfc in cls.components[ComponentType.BFC]) {
                                double fcRange = ((AurCompBFC)bfc.component).rangeMax;
                                double fcTrackingSpeed = ((AurCompBFC)bfc.component).trackingSpeed;

                                double trackingSpeed = Math.Min(weaponTrackingSpeed, fcTrackingSpeed);

                                double dmg = GetWeaponAnalysisValue(weapon, dist, fcRange, shotInterval, trackingSpeed, weaponTargetSpeed, cls.tonnage / 50, cls.cost, cls.mineralCost);

                                dmgPotential = Math.Max(dmg * weaponType.number, dmgPotential);
                            }

                            dmgSum += dmgPotential;
                        }


                        if (dmgSum > 0) {
                            analyzedClasses[cls].Values.Add(dmgSum);
                        } else {
                            break;
                        }
                    }
                }
            }

            chrtWeapons.AxisX[0].Labels = labelsX;
            chrtWeapons.UpdaterState = UpdaterState.Running;
            chrtWeapons.Update();
        }

        double GetWeaponAnalysisValue(AurCompWeapon weapon, double dist, double fcRange, int shotInterval, double fcTrackingSpeed, double targetSpeed, double hs, double cost, double[] mineralCost) {
            double dmg = getDmgAtDist(weapon.damage, weapon.rangeMod, weapon.rangeMax, dist) * weapon.shots;

            if (optAnalyzeShots.IsChecked == true) {
                if (dmg > 0) {
                    dmg = weapon.shots;
                } else {
                    dmg = 0;
                }
            }

            dmg *= weapon.toHitMod;

            if (chkAnalyzePer5s.IsChecked == true) {
                dmg /= shotInterval;
            }

            if (chkAnalyzePerHS.IsChecked == true) {
                dmg /= hs;
            } else if (chkAnalyzePerCost.IsChecked == true) {
                dmg /= cost;
            } else if (chkAnalyzePerPrice.IsChecked == true) {
                double price = GetCombinedMineralPrice(mineralCost);

                dmg /= price;
            }

            if (chkAnalyzeWithFC.IsChecked == true) {
                dmg *= (1.0 - dist / fcRange);

                if (fcTrackingSpeed < targetSpeed) {
                    dmg *= (fcTrackingSpeed / targetSpeed);
                }
            }

            return dmg;
        }

        double prospectAmountCap = 10000000;
        double prospectMinAmount = 10000;
        int prospectResCnt = 50;
        int prospectChrtCnt = 15;

        void UpdateProspect() {
            chrtProspect.UpdaterState = UpdaterState.Paused;

            Func<AurBody, bool> filterFunc = (body) => {
                if(cmbProspectTarget.SelectedIndex != 0) {
                    return (body.populations.Count > 0) == (cmbProspectTarget.SelectedIndex == 1);
                }

                return true;
            };

            Func<double[], double> amountFunc = GetMineralSelectorFunc(cmbProspectMineral);
            Func<AurBody, double> valueFunc = (body) => {
                double[] mineralsAdjsuted = new double[Enum.GetValues(typeof(MineralType)).Length];
                for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++) {
                    switch (cmbProspectFor.SelectedIndex) {
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
            };

            ChartValues<double> values = new ChartValues<double>();
            List<string> labels = new List<string>();
            chrtProspect.Series[0].Values = values;
            chrtProspect.AxisY[0].Labels = labels;
            lstProspect.Items.Clear();

            int cnt = 0;
            foreach (AurBody body in (from dictRec in curGame.bodyIdx where (dictRec.Value.hasMinerals && dictRec.Value.isSurveyed[curRace] && filterFunc(dictRec.Value)) orderby valueFunc(dictRec.Value) descending select dictRec.Value).Take(prospectResCnt)) {
                double value = valueFunc(body);
                string name = body.GetName(curRace);

                if (cnt < prospectChrtCnt) {
                    values.Add(value);
                    labels.Add(name);
                }

                ProspectListItem listItem = new ProspectListItem();
                listItem.Name = name;
                listItem.Value = value > 1000 ? (value / 1000).ToString("N0") + "K" : value.ToString("F2");

                for (int i = 0; i < Enum.GetValues(typeof(MineralType)).Length; i++) {
                    listItem.MineralValues[i] = body.minerals[i].ToString("N0") + " (" + body.mineralsAcc[i].ToString("N1") + ")";
                }

                lstProspect.Items.Add(new ListViewItem() { Content = listItem, HorizontalContentAlignment = HorizontalAlignment.Right });
                cnt++;
            }

            chrtProspect.UpdaterState = UpdaterState.Running;
            chrtProspect.Update();
        }

        void RecalculateStockpilePie() {
            pieStockpiles.UpdaterState = UpdaterState.Paused;

            Func<double[], double> amountFunc = GetMineralSelectorFunc(cmbStockpilePieMineral);

            int seriesIdx = 0;
            foreach (AurPop pop in (from pop in curRace.populations where amountFunc(pop.minerals) > 0 orderby amountFunc(pop.minerals) descending select pop)) {
                ChartValues<double> val = new ChartValues<double>();
                val.Add(amountFunc(pop.minerals));

                PieSeries pieSeries;
                if (pieStockpilesSeries.Count <= seriesIdx) {
                    pieSeries = new PieSeries();
                    pieStockpilesSeries.Add(pieSeries);
                } else {
                    pieSeries = (PieSeries)pieStockpilesSeries[seriesIdx];
                }

                pieSeries.Values = val;
                pieSeries.Title = "";
                pieSeries.LabelPoint = (chartPoint) => { return pop.name + "\n" + mineralLabelFunc(chartPoint.Y); };
                pieSeries.DataLabels = amountFunc(pop.minerals) / amountFunc(curRace.minerals) > 0.07;
                seriesIdx++;
            }

            while (pieStockpilesSeries.Count > seriesIdx) {
                pieStockpilesSeries.RemoveAt(seriesIdx);
            }

            pieStockpiles.UpdaterState = UpdaterState.Running;
            pieStockpiles.Update();
        }

        private void cmbGame_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(cmbGame.SelectedIndex == -1) {
                return;
            }
            curGame = aurData.Games[cmbGame.SelectedIndex];

            ReloadRaceCmb();
        }

        private void cmbRace_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (cmbRace.SelectedIndex == -1) {
                return;
            }
            curRace = curGame.Races[cmbRace.SelectedIndex];

            OnRaceSelected();
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e) {
            if(txtOutput.Visibility != Visibility.Visible) {
                txtOutput.Visibility = Visibility.Visible;
            } else {
                txtOutput.Visibility = Visibility.Hidden;
            }
        }

        private void cmbPriceMod_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (curRace != null) {
                CalcMineralPrice();
            }
        }

        private void cmbStockpileMineral_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (curRace != null) {
                RecalculateStockpilePie();
            }
        }

        private void cmbProspect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(curRace != null) {
                UpdateProspect();
            }
        }

        private void cmbProspectFor_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (cmbProspectFor.SelectedIndex == 0) {
                lblProspectLimit.Content = "Min Amount";
                txtProspectLimit.Text = prospectMinAmount.ToString("F0");
            } else {
                lblProspectLimit.Content = "Amount Cap";
                txtProspectLimit.Text = prospectAmountCap.ToString("F0");
            }

            cmbProspect_SelectionChanged(sender, e);
        }

        private void txtProspectLimit_TextChanged(object sender, TextChangedEventArgs e) {
            double val;
            if (double.TryParse(txtProspectLimit.Text, out val)) {
                if (cmbProspectFor.SelectedIndex == 0) {
                    prospectMinAmount = val;
                } else {
                    prospectAmountCap = val;
                }
            }

            if (curRace != null) {
                UpdateProspect();
            }
        }

        private void txtWeapFCRange_TextChanged(object sender, TextChangedEventArgs e) {
            double val;
            if (double.TryParse(txtWeapFCRange.Text, out val) && val > 0) {
                weaponFCRange = val;
            }

            if (curRace != null) {
                UpdateWeaponAnalysis();
            }
        }

        private void txtWeapFCTrack_TextChanged(object sender, TextChangedEventArgs e) {
            double val;
            if (double.TryParse(txtWeapFCTrack.Text, out val) && val > 0) {
                weaponFCSpeed = val;
            }

            if (curRace != null && chkAnalyzeWithFC.IsChecked == true) {
                UpdateWeaponAnalysis();
            }
        }

        private void txtWeapShipSpeed_TextChanged(object sender, TextChangedEventArgs e) {
            double val;
            if (double.TryParse(txtWeapShipSpeed.Text, out val) && val > 0) {
                weaponShipSpeed = val;
            }

            if (curRace != null && chkAnalyzeWithFC.IsChecked == true) {
                UpdateWeaponAnalysis();
            }
        }

        private void txtWeapTargetSpeed_TextChanged(object sender, TextChangedEventArgs e) {
            double val;
            if (double.TryParse(txtWeapTargetSpeed.Text, out val) && val > 0) {
                weaponTargetSpeed = val;
            }

            if (curRace != null && chkAnalyzeWithFC.IsChecked == true) {
                UpdateWeaponAnalysis();
            }
        }

        private void chkPerCost_Checked(object sender, RoutedEventArgs e) {
            if(sender != chkAnalyzePerHS) {
                chkAnalyzePerHS.IsChecked = false;   
            }
            if(sender != chkAnalyzePerCost) {
                chkAnalyzePerCost.IsChecked = false;   
            }
            if(sender != chkAnalyzePerPrice) {
                chkAnalyzePerPrice.IsChecked = false;   
            }
        }

        private void optWeaponAnalysis_Checked(object sender, RoutedEventArgs e) {
            if (curRace != null) {
                UpdateWeaponAnalysis();
            }
        }

        private void optAnalyzeWeapon_Checked(object sender, RoutedEventArgs e) {
            // null check necessary during initialization
            if (cmbWeapFC != null) {
                bool analyzeWeapon = optAnalyzeWeapon.IsChecked == true;
                cmbWeapFC.IsEnabled = analyzeWeapon;
                txtWeapFCRange.IsEnabled = analyzeWeapon;
                txtWeapFCTrack.IsEnabled = analyzeWeapon;
                txtWeapShipSpeed.IsEnabled = analyzeWeapon;
            }

            if (curRace != null) {
                PopulateWeaponList();
            }
        }

        private void chkAnalyzeObsolete_Click(object sender, RoutedEventArgs e) {
            if (curRace != null) {
                PopulateWeaponList();
            }
        }

        private void cmbWeapFC_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(curRace != null && cmbWeapFC.SelectedIndex >= 0) {
                AurCompBFC bfc = (AurCompBFC)curRace.knownCompIdx[ComponentType.BFC][cmbWeapFC.SelectedIndex].component;

                txtWeapFCRange.Text = bfc.rangeMax.ToString();
                txtWeapFCTrack.Text = bfc.trackingSpeed.ToString();
            }
        }
    }
}
