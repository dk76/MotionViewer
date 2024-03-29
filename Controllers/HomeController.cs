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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;




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
                if(!dir.Replace("$","/").StartsWith(root))
                {
                    Response.StatusCode=StatusCodes.Status400BadRequest;
                    return model;      
                }    

                root=dir;
                root=root.Replace("$","/");
            }

            

            var dirs=Directory.GetDirectories(root).ToList();
            dirs.Sort();

            foreach(var item in dirs)
                model.directorys.Add(item);


            var files=Directory.GetFiles(root).Where(  item=> (item.EndsWith(".mp4")||item.EndsWith(".avi")) ).ToList();
            files.Sort();
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

        [Route("Home/preview/{file}")]
        async public Task<IActionResult> GetPreview(string file)
        {

            file=file.Replace("$","/");
            if((file.EndsWith(".mp4")) ||(file.EndsWith(".avi")) )
            {
              

                

                string tempFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".jpg"; 
                string ffmpeg=_configuration["ffmpegPath"];

                Process process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = ffmpeg;
                process.StartInfo.Arguments = "-ss 0  -i "+file+" -qscale:v 4 -frames:v 1 "+tempFileName;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                process.Start();
                process.WaitForExit();


                var memory = new MemoryStream();  
                using (var stream = new FileStream(tempFileName, FileMode.Open))  
                {  
                    await stream.CopyToAsync(memory);  
                }  
                memory.Position = 0;  
                return File(memory, "image/jpeg", Path.GetFileName(tempFileName));  

            }

            return Content("data");

        }


        [Route("Home/file/{file}")]

        public FileStreamResult GetFile(string file)
        {
            file=file.Replace("$","/");
            //Response.StatusCode=StatusCodes.Status304NotModified;
            //Response.Headers.Add("Accept-Ranges","bytes");
            //Response.Headers.Add("ETag","1d663561d12a96c");
            //Response.Headers.Add("Last-Modified","Sun, 26 Jul 2020 14:07:36 GMT");

            
            if(file.EndsWith(".mp4"))
            {

            /*   var memory = new MemoryStream();  
                using (var stream = new FileStream(file, FileMode.Open))  
                {  
                    await stream.CopyToAsync(memory);  
                }  
            memory.Position = 0;  
            
            return File(memory, "video/mp4");  */
            return new VideoStreamResult(new FileStream(file, FileMode.Open),"video/mp4");


            }
            else
            if(Path.GetExtension(file)==".avi")
            {
                var files=HttpContext.RequestServices.GetService<IFileDecodedNames>();

                string tempFileName=files.Get(file);


                if((tempFileName=="") || (!System.IO.File.Exists(tempFileName)))
                {
 
                        if(tempFileName!="")files.Delete(file);

                        tempFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".mp4"; 
                        string ffmpeg=_configuration["ffmpegPath"];

                        Process process = new Process();
                        // Configure the process using the StartInfo properties.
                        process.StartInfo.FileName = ffmpeg;
                        process.StartInfo.Arguments = "-i "+file+" "+tempFileName;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                        process.Start();
                        process.WaitForExit();

                        files.Add(file,tempFileName);


                      


                }  

                return new VideoStreamResult(new FileStream(tempFileName, FileMode.Open),"video/mp4");


            }

            return null;
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
