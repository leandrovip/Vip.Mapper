using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace Vip.Mapper.Tests
{
    public class TestBase
    {
        [TearDown]
        public void TearDown()
        {
            AutoMapper.Cache.ClearAllCaches();
        }
    }
}