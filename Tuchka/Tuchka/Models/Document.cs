﻿using System;

namespace Tuchka.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long? FileSize { get; set; }
        public string Title { get; set; }
        public string OwnerId { get; set; }
        public DateTime Date { get; set; }

    }
}
