using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Vip.Mapper.Tests
{
    [TestFixture]
    public class SimpleTypeConversionTests : TestBase
    {
        public class PersonWithFields
        {
            public int Id;
            public string FirstName;
            public string LastName;
        }

        public class PersonWithProperties
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        [Test]
        public void Can_Map_Matching_Field_Names_With_Different_Types()
        {
            // Arrange
            const int id = 1;
            const string firstName = "Bob";
            const string lastName = "Smith";

            var dictionary = new Dictionary<string, object>
            {
                {"Id", double.Parse("1.245698", CultureInfo.InvariantCulture)},
                {"FirstName", firstName},
                {"LastName", lastName}
            };

            // Act
            var customer = AutoMapper.Map<PersonWithFields>(dictionary);

            // Assert
            Assert.NotNull(customer);
            Assert.That(customer.Id == id);
            Assert.That(customer.FirstName == firstName);
            Assert.That(customer.LastName == lastName);
        }

        [Test]
        public void Can_Map_Matching_Property_Names_With_Different_Types()
        {
            // Arrange
            const int id = 1;
            const string firstName = "Bob";
            const string lastName = "Smith";

            var dictionary = new Dictionary<string, object>
            {
                {"Id", double.Parse("1.245698", CultureInfo.InvariantCulture)},
                {"FirstName", firstName},
                {"LastName", lastName}
            };

            // Act
            var customer = AutoMapper.Map<PersonWithProperties>(dictionary);

            // Assert
            Assert.NotNull(customer);
            Assert.That(customer.Id == id);
            Assert.That(customer.FirstName == firstName);
            Assert.That(customer.LastName == lastName);
        }
    }
}