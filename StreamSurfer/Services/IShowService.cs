﻿using StreamSurfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamSurfer.Services
{
    public interface IShowService
    {
        string ConvertToShowSearch(string query);
        string ConvertToMovieSearch(string query);
        string ConvertToShowDetail(int id);
        string ConvertToMovieDetail(int id);
        string ConvertToServices(int id);
        string GetShows(int limit);
        string GetEpisodes(int id, int limit, int offset);
        string GetSources();
        string GetGenres();
        string ConvertToMovieImage(int id);
        Task<Show> GetShowDetails(int? id, PostgresDataContext _context, IWebRequestHandler webRequest);
        Task<Movie> GetMovieDetails(int? id, PostgresDataContext _context, IWebRequestHandler webRequest);
    }
}
