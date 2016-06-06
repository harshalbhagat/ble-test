using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;  

// ReSharper disable MemberCanBePrivate.Global

namespace UnitTestProject1
    {
    [TestClass]
    public class UnitTest1
        {
        [TestMethod]
        public void TestMethod1()
            {
            var calories = CalculateCaloriesPerLbForSpeedMiles( 4, 3600 );
            Debug.WriteLine( 215 * calories  );
            }

        public static double CalculateCaloriesPerKgForSpeedMeters( double metersPerSecond, ushort duration )
            {
            double caloriesPerKgPerHour = 1.1051 * metersPerSecond * metersPerSecond + 0.9665 * metersPerSecond;
            return caloriesPerKgPerHour * duration / ( 60 * 60 );
            }

        public static double CalculateCaloriesPerLbForSpeedMiles( double milesPerHour, ushort duration )
            {
            double metersPerSecond = ( milesPerHour * 1600 ) / 3600;
            return CalculateCaloriesPerKgForSpeedMeters( metersPerSecond, duration ) / 2.2;
            }
        }
    }
