﻿using System.ComponentModel.DataAnnotations;

namespace Domain
{
    /// <summary>
    /// Models key-value pairs for deployment properties
    /// </summary>
    public class DeploymentProperty
    {
        [Key]
        public int Id { get; set; }
        public string PropertyKey { get; set; }
        public string PropertyValue { get; set; }
        public string NewPropertyValue { get; set; }
    }
}
