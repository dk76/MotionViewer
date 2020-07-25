﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotionViewer.Models;
using Microsoft.Extensions.Configuration;


namespace MotionViewer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
         private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration=configuration;
        }


        private IndexViewModel GetIndexViewModel(string dir="")
        {

            string root=_configuration["rootArchive"];

            var model=new IndexViewModel();

            if(dir!=""){
                root=dir;
                root=root.Replace("$","/");
            }

            var dirs=Directory.GetDirectories(root);

            foreach(var item in dirs)
                model.directorys.Add(item);


            var files=Directory.GetFiles(root);
            foreach(var item in files)
                model.files.Add(item);


            return model;
        }

        public IActionResult Index()
        {
            return View(GetIndexViewModel());
        }


        [Route("Home/dir/{dir}")]
        public IActionResult Index(string dir)
        {
            
            return View(GetIndexViewModel(dir));
        }

        [Route("Home/file/{file}")]
        async public Task<IActionResult> GetFile(string file)
        {
            file=file.Replace("$","/");
            if(file.EndsWith(".mp4"))
            {

               var memory = new MemoryStream();  
                using (var stream = new FileStream(file, FileMode.Open))  
                {  
                    await stream.CopyToAsync(memory);  
                }  
            memory.Position = 0;  
            return File(memory, "video/mp4", Path.GetFileName(file));  
            }
            else
            if(Path.GetExtension(file)==".avi")
            {
                string tempFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".mp4"; 
                string ffmpeg=_configuration["ffmpegPath"];

                Process process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = ffmpeg;
                process.StartInfo.Arguments = "-i "+file+" "+tempFileName;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                process.Start();
                process.WaitForExit();


                var memory = new MemoryStream();  
                using (var stream = new FileStream(tempFileName, FileMode.Open))  
                {  
                    await stream.CopyToAsync(memory);  
                }  
                memory.Position = 0;  
                return File(memory, "video/mp4", Path.GetFileName(tempFileName));  


            }

            return Content("data");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}