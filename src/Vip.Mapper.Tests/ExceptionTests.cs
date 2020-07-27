using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Vip.Mapper.Tests
{
    [TestFixture]
    public class ExceptionTests : TestBase
    {
        public class Person
        {
            public int PersonId;
            public string FirstName;
            public string LastName;
        }

        [Test]
        public void Will_Throw_An_Exception_If_The_Type_Is_Not_Dynamic()
        {
            // Arrange
            var someObject = new object();

            // Act
            TestDelegate test = () => AutoMapper.MapDynamic<Person>(someObject);

            // Assert
            Assert.Throws<ArgumentException>(test);
        }

        [Test]
        public void Will_Not_Throw_An_Exception_If_The_List_Items_Are_Not_Dynamic()
        {
            // Arrange
            var someObjectList = new List<object> {null};

            // Act
            TestDelegate test = () => AutoMapper.MapDynamic<Person>(someObjectList);

            // Assert
            Assert.DoesNotThrow(test);
        }

        [Test]
        public void Will_Return_An_Empty_List_Of_The_Requested_Type_When_Passed_An_Empty_List()
        {
            // Arrange
            var someObjectList = new List<object>();

            // Act
            var list = AutoMapper.MapDynamic<Person>(someObjectList);

            // Assert
            Assert.NotNull(list);
            Assert.That(list.Count() == 0);
        }
    }
}