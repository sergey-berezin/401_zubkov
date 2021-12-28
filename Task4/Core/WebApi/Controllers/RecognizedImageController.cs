using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.ObjectRecognitionComponent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.WebApi.Context;
using Core.WebApi.Infrastructure.Converters;
using Core.WebApi.Models;
using Core.WebApi.Models.Entities;

namespace Core.WebApi.Controllers
{
    [ApiController]
    [Route("api/recognizedimage")]
    public class RecognizedImageController : ControllerBase
    {
        private readonly RecognizedImagesDb _context;

        public RecognizedImageController(RecognizedImagesDb context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecognizedImage>>> GetAllRecognizedImages()
        {
            var recognizedImages = await _context.RecognizedImages.ToArrayAsync();

            var results = new List<RecognizedImage>();
            foreach (var recognizedImage in recognizedImages)
            {
                results.Add(new RecognizedImage(recognizedImage));
            }

            return Ok(results);
        }

        [HttpPost("recognize")]
        public async Task<ActionResult<IEnumerable<RecognizedImage>>> GetRecognizedImage([FromBody] byte[] imageData)
        {
            var recognizer = new ObjectRecognizer(ObjectRecognizer.ONNX_MODEL_PATH);

            var results = new List<RecognizedImage>();

            await foreach (var result in recognizer.RunObjectRecognizer(imageData))
            {
                var newImage = new RecognizedImage(imageData, result);

                var category = await _context.Categories
                    .SingleOrDefaultAsync(item => item.CategoryName == newImage.Label)
                    .ConfigureAwait(false);

                if (category is null)
                {
                    category = new CategoryEntity() { CategoryName = newImage.Label };
                    _context.Entry(category).State = EntityState.Added;
                }

                var candidatesRecognizedImage = await _context.RecognizedImages
                    .Where(item => item.CategoryEntity == category && item.BBox == newImage.BBox)
                    .ToArrayAsync().ConfigureAwait(false);


                RecognizedImageEntity recognizedImage = new();

                var inDb = false;
                foreach (var candidateRecognizedImage in candidatesRecognizedImage)
                {
                    if (candidateRecognizedImage.SerializedImage.SequenceEqual(newImage.ImageByteData))
                    {
                        inDb = true;
                        recognizedImage = candidateRecognizedImage;
                        break;
                    }
                }

                if (!inDb)
                {
                    recognizedImage = ResultRecognitionDbDataConverter.Convert(newImage, category);
                    _context.Entry(recognizedImage).State = EntityState.Added;
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                }

                results.Add(new RecognizedImage(recognizedImage));
            }

            return Ok(results);
        }

        [HttpDelete("remove/{id}")]
        public async Task<ActionResult> Remove(int id)
        {
            var itemToRemove = await _context.RecognizedImages
                .SingleOrDefaultAsync(item => item.Id == id)
                .ConfigureAwait(false);

            if (itemToRemove is null)
            {
                return NotFound();
            }

            var category = itemToRemove.CategoryEntity;

            _context.RecognizedImages.Remove(itemToRemove);
            await _context.SaveChangesAsync();

            if (category.Images.Count == 0)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

    }
}
