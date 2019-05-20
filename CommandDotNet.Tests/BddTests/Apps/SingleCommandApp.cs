using CommandDotNet.Attributes;
using CommandDotNet.Tests.Utils;

namespace CommandDotNet.Tests.BddTests.Apps
{
    public class SingleCommandApp
    {
        [InjectProperty]
        public TestWriter Writer { get; set; }
            
        [InjectProperty]
        public TestOutputs TestOutputs { get; set; }
            
            
        public void Add(int x, int y)
        {
            this.Writer.Write($"{x}+{y}={x+y}");
            this.TestOutputs.Capture(new AddResults{X = x, Y = y});
        }

        public class AddResults
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}