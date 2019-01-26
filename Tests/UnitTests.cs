using System.IO;
using System.Reflection;
using Core;
using NUnit.Framework;

namespace Tests
{
    public class UnitTests
    {
        [Test]
        public void Test()
        {
            var bin = Directory.GetCurrentDirectory();
            var directory = Directory.GetParent(bin).Parent.Parent.FullName;
            Program.Main(new []
            {
                Path.Combine(directory, "diff.txt"),
                Path.Combine(directory, "SonarCube.xml"),
                Path.Combine(bin, "actual_results.txt"),
            });

            Assert.AreEqual(
                File.ReadAllText(Path.Combine(directory, "expected_result.txt")),
                File.ReadAllText(Path.Combine(bin, "actual_results.txt")));
        }
    }
}