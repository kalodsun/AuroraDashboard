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

namespace AuroraDashboard
{
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

        private void CalcMineralPrice() {
            MineralPriceMod priceMod = MineralPriceMod.noMod;
            switch (cmbPriceMod.SelectedIndex) {
            case 1:
                priceMod = MineralPriceMod.usefulnessMod;
                break;
            }

            double mineralSum = 0;
            for (int i = 0; i < curRace.minerals.Length; i++) {
                mineralSum += curRace.minerals[i] * priceMod.priceMult[i];
            }

            double avgMineralAmt = mineralSum / curRace.minerals.Length;

            for (int i = 0; i < curRace.minerals.Length; i++) {
                mineralPrices[i] = avgMineralAmt / curRace.minerals[i];
            }

            OnPriceChanged();
        }

        public SeriesCollection pieStockpilesSeries = new SeriesCollection();

        public MainWindow()
        {
            InitializeComponent();

            cmbStockpileMineral.Items.Add("All minerals (amount)");
            cmbStockpileMineral.Items.Add("All minerals (price)");
            foreach(string name in Enum.GetNames(typeof(MineralType))) {
                cmbStockpileMineral.Items.Add(name);
            }
            cmbStockpileMineral.SelectedIndex = 0;

            pieStockpiles.Series = pieStockpilesSeries;
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
            if(cmbStockpileMineral.SelectedIndex == 1) {
                RecalculateStockpilePie();
            }
        }

        void OnRaceSelected() {
            MineralPriceMod priceMod = MineralPriceMod.noMod;
            
            CalcMineralPrice();

            PopulateMineralTab();
        }

        void PopulateMineralTab() {
            RecalculateStockpilePie();
        }

        void RecalculateStockpilePie() {
            pieStockpilesSeries.Clear();

            Func<AurPop, double> amountFunc;
            if(cmbStockpileMineral.SelectedIndex > 1) {
                amountFunc = pop => pop.minerals[cmbStockpileMineral.SelectedIndex-2];
            } else if(cmbStockpileMineral.SelectedIndex == 1) {
                amountFunc = pop => {
                    double result = 0;
                    for (int i = 0; i < pop.minerals.Length; i++) {
                        result += pop.minerals[i] * mineralPrices[i];
                    }
                    return result;
                };
            } else {
                amountFunc = pop => pop.minerals.Sum();
            }

            foreach (AurPop pop in (from pop in curRace.populations where amountFunc(pop) > 0 orderby amountFunc(pop) descending select pop)) {
                ChartValues<double> val = new ChartValues<double>();
                val.Add(amountFunc(pop));

                PieSeries pieSeries = new PieSeries();
                pieSeries.Values = val;
                pieSeries.Title = pop.name;
                pieStockpilesSeries.Add(pieSeries);
            }
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
    }
}
