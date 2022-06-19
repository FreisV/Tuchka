using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Tuchka.DbContext;
using Tuchka.Models;
using Microsoft.AspNetCore.Identity;
using Tuchka.IdentityAuth;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Authorization;

namespace Tuchka.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        // GET: api/Document
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
        {
            return await _context.Documents.ToListAsync();
        }

        // GET api/Document/list/{username}
        [HttpGet]
        [Route("list/{username}")]
        public async Task<ActionResult<IEnumerable<Document>>> GetUserDocuments(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            return await _context.Documents.Where(doc => doc.OwnerId == user.Id).ToListAsync();

        }

        // GET: api/Documents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(int id, string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            var document = await _context.Documents.FindAsync(id);

            if (user.Id != document.OwnerId)
                return StatusCode(StatusCodes.Status403Forbidden, new Response { Status = "Error", Message = "It isn't your file" });


            if (document == null)
            {
                return NotFound();
            }

            return document;
        }


        // PUT: api/Documents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDocument(int id, Document document)
        {
            if (id != document.Id)
            {
                return BadRequest();
            }

            _context.Entry(document).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPut]
        [Route("upload/{id}")]
        public async Task<IActionResult> UploadInfo(int id, UploadInfoModel model)
        {
            var document = await _context.Documents.FindAsync(id);
            var user = await _userManager.FindByNameAsync(model.Username);

            if (document == null)
                return NotFound();

            if (user == null)
                return NotFound();

            document.Title = document.FileName;
            document.OwnerId = user.Id;

            string newFileName = $"{user.UserName}_{document.Date.Ticks}_{document.FileName}";
            var rootPath = Path.Combine(_environment.ContentRootPath, "Resources", "Documents");

            System.IO.File.Move(Path.Combine(rootPath, document.FileName), Path.Combine(rootPath, newFileName));

            document.FileName = newFileName;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { Status = "Success", Message = "File uploaded" });
        }

        // POST: api/Documents
        [HttpPost]
        public async Task<ActionResult<Document>> PostDocument(Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDocument", new { id = document.Id }, document);
        }


        // POST api/Documents/upload
        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Upload(List<IFormFile> files)
        {
            long size = files.Sum(f => f.Length);

            var rootPath = Path.Combine(_environment.ContentRootPath, "Resources", "Documents");

            List<int> filesId = new List<int>();

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            foreach (var file in files)
            {
                var filePath = Path.Combine(rootPath, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    DateTime uploadDate = DateTime.Now;

                    var document = new Document
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        Date = uploadDate,
                    };

                    await file.CopyToAsync(stream);

                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                    var uploatedDocument = await _context.Documents.FirstOrDefaultAsync(doc => doc.FileName == document.FileName && doc.Date == uploadDate);

                    filesId.Add(uploatedDocument.Id);
                }
            }

            return Ok(new { count = files.Count, size, filesId});
        }

        // POST api/Documents/download
        [HttpGet]
        [Route("download/{id}")]
        public async Task<ActionResult> Download(int id)
        {
            var provider = new FileExtensionContentTypeProvider();

            var document = await _context.Documents.FindAsync(id);

            if (document == null)
                return NotFound();

            var file = Path.Combine(_environment.ContentRootPath, "Resources", "Documents", document.FileName);

            string contentType = document.ContentType;

            byte[] fileBytes;
            if (System.IO.File.Exists(file))
            {
                fileBytes = System.IO.File.ReadAllBytes(file);
            }
            else
            {
                return NotFound();
            }

            return File(fileBytes, contentType, document.Title);
        }


        // DELETE: api/Documents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {

            var document = await _context.Documents.FindAsync(id);

            if (document == null)
                return NotFound();

            var documents = await _context.Documents.Where(doc => doc.Title == document.Title && doc.OwnerId == document.OwnerId && doc.ContentType == document.ContentType).ToListAsync();

            foreach (var doc in documents)
            {
                var file = Path.Combine(_environment.ContentRootPath, "Resources", "Documents", doc.FileName);

                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }

                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync();

            }

            return Ok( new {Status = "Success", Message= "Files deleted succesfull"});
        }

        [HttpDelete]
        [Route("{id}/one")]
        public async Task<IActionResult> DeleteOneDocument(int id)
        {

            var document = await _context.Documents.FindAsync(id);

            if (document == null)
                return NotFound();

            var file = Path.Combine(_environment.ContentRootPath, "Resources", "Documents", document.FileName);

            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { Status = "Success", Message = "File deleted succesfull" });
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete]
        [Route("admin/{id}")]
        public async Task<IActionResult> AdminDeleteDocument(int id)
        {
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
                return NotFound();

            var file = Path.Combine(_environment.ContentRootPath, "Resources", "Documents", document.FileName);

            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { Status = "Success", Message = "File deleted succesfull" });
        }


        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.Id == id);
        }
    }
}
