using FM.ConsulInterop.Config;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace FM.ConsulInterop.Tests
{
    public class ConfigValidationTest
    {  
        [Fact]
        public void LocalServiceConfig_ConsulTagIsNullOrEmpty()
        {
            var localService = new ConsulLocalServiceConfig();

            Assert.Throws<Exception>(() => {
                localService.Validation();
            });
        }

        [Fact]
        public void LocalServiceConfig_ConsulTagErrorFormat()
        {
            var localService = new ConsulLocalServiceConfig {
             ConsulTags="123232"
            };

            Assert.Throws<FormatException>(() =>
            {
                localService.Validation();
            });
        }

        [Fact]
        public void LocalServiceConfig_ConsulTagErrorFormatV()
        {
            var localService = new ConsulLocalServiceConfig
            {
                ConsulTags = "v1.2.3"
            };

            Assert.Throws<FormatException>(() =>
            {
                localService.Validation();
            });

        }

        [Fact]
        public void LocalServiceConfig_ConsulTagValid()
        {
            var localService = new ConsulLocalServiceConfig
            {
                ConsulTags = "v-1.2.2"
            };

            localService.Validation();
        }
    }
}
