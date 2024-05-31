﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MonitoringService.Domain.Models
{
    public class SpecDetails
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Revision { get; set; }
        public string Date { get; set; }
        public string Area { get; set; }
        public string Purpose { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string Folder { get; set; }
    }
}