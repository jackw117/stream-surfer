﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamSurfer.Models
{
    public class Genre
    {
        public int ID { get; set; }
        public string Title { get; set; }

        public List<ShowGenre> ShowGenre { get; set; }
        public List<MovieGenre> MovieGenre { get; set; }
    }
}
