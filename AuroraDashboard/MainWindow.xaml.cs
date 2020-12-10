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

        public Func<double, string> mineralLabelFunc = (value) => (value / 1000).ToString("N0") + "K";

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
                    double result = 0;
                    for (int i = 0; i < minerals.Length; i++) {
                        result += minerals[i] * mineralPrices[i];
                    }
                    return result;
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
                aurData = dbReader.ReadDB(@"Data Source=D:\Projects\AuroraDashboard\AuroraDB190.db;", progress);
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
        }

        void OnRaceSelected() {
            MineralPriceMod priceMod = MineralPriceMod.noMod;
            
            CalcMineralPrice();

            PopulateMineralTab();

            UpdateProspect();
        }

        void PopulateMineralTab() {
            RecalculateStockpilePie();

            chrtMineralStockpile.Series[0].Values = new ChartValues<double>(curRace.minerals);
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

                lstProspect.Items.Add(new ListViewItem() { Content = listItem });
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
    }
}
