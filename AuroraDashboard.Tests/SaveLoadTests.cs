using NUnit.Framework;
using AuroraDashboard;
using System.IO;
using System.Collections.Generic;

namespace AuroraDashboard.Tests
{
    [TestFixture]
    public class SaveLoadTests
    {
        [Test]
        public void SaveAndLoad_ShouldPreserveData()
        {
            // Arrange
            var data = new AuroraData();
            data.Hulls.Add(new AurHull { ID = 1, name = "Test Hull", abbrev = "TH" });
            data.hullIdx.Add(1, data.Hulls[0]);

            var game = new AurGame { ID = 1, name = "Test Game" };
            data.Games.Add(game);

            var race = new AurRace { ID = 1, name = "Test Race" };
            game.Races.Add(race);
            game.raceIdx.Add(1, race);

            var filePath = Path.Combine(Path.GetTempPath(), "test_aurora_data.json");

            // Act
            data.Save(filePath);
            var loadedData = AuroraData.Load(filePath);

            // Assert
            Assert.IsNotNull(loadedData);
            Assert.AreEqual(data.Hulls.Count, loadedData.Hulls.Count);
            Assert.AreEqual(data.Hulls[0].name, loadedData.Hulls[0].name);
            Assert.AreEqual(data.Games.Count, loadedData.Games.Count);
            Assert.AreEqual(data.Games[0].name, loadedData.Games[0].name);
            Assert.AreEqual(data.Games[0].Races.Count, loadedData.Games[0].Races.Count);
            Assert.AreEqual(data.Games[0].Races[0].name, loadedData.Games[0].Races[0].name);

            // Clean up
            File.Delete(filePath);
        }
    }
}
