﻿namespace ElAhorrador.Models
{
    public class ScrapeConfiguration : AmazonProductConfiguration
    {
        public string ProductsPath { get; set; }
        public string BaseProductUrl { get; set; }
    }
}