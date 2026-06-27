// AnimalService.cs
using AnimalShelterAI.Core.DTOs;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnimalShelterAI.Services
{
    public class AnimalService
    {
        private readonly ShelterDbContext _context;

        public AnimalService(ShelterDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AnimalDto>> GetAnimalsAsync()
        {
            var animals = await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .OrderByDescending(a => a.admissiondate)
                .ToListAsync();

            return animals.Select(MapToDto);
        }

        public async Task<AnimalDto?> GetAnimalByIdAsync(int id)
        {
            var animal = await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .FirstOrDefaultAsync(a => a.animalid == id);

            return animal == null ? null : MapToDto(animal);
        }

        public async Task<animal?> GetFullAnimalByIdAsync(int id)
        {
            return await _context.animals
                .Include(a => a.type)
                .Include(a => a.breed)
                .FirstOrDefaultAsync(a => a.animalid == id);
        }

        public async Task<IEnumerable<animaltype>> GetAnimalTypesAsync()
        {
            return await _context.animaltypes
                .OrderBy(t => t.typename)
                .ToListAsync();
        }

        // Главный метод для пород
        // Добавьте этот метод в AnimalService.cs если его нет
        public async Task<IEnumerable<breed>> GetBreedsByTypeAsync(int typeId)
        {
            return await _context.breeds
                .Where(b => b.typeid == typeId)
                .OrderBy(b => b.breedname)
                .ToListAsync();
        }

        public async Task<bool> CreateAnimalAsync(AnimalCreateDto dto)
        {
            try
            {
                var animal = new animal
                {
                    name = dto.Name,
                    typeid = dto.Typeid,
                    breedid = dto.Breedid,
                    gender = dto.Gender,
                    age = dto.Age,
                    weight = dto.Weight,
                    color = dto.Color,
                    description = dto.Description,
                    photourl = dto.Photourl,
                    admissiondate = dto.Admissiondate,
                    status = dto.Status,
                    healthstatus = dto.Healthstatus
                };

                await _context.animals.AddAsync(animal);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAnimalAsync(int id, AnimalCreateDto dto)
        {
            try
            {
                var animal = await _context.animals.FindAsync(id);
                if (animal == null) return false;

                animal.name = dto.Name;
                animal.typeid = dto.Typeid;
                animal.breedid = dto.Breedid;
                animal.gender = dto.Gender;
                animal.age = dto.Age;
                animal.weight = dto.Weight;
                animal.color = dto.Color;
                animal.description = dto.Description;
                animal.photourl = dto.Photourl;
                animal.admissiondate = dto.Admissiondate;
                animal.status = dto.Status;
                animal.healthstatus = dto.Healthstatus;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private AnimalDto MapToDto(animal a)
        {
            return new AnimalDto
            {
                Animalid = a.animalid,
                Name = a.name,
                Typename = a.type?.typename,
                Breedname = a.breed?.breedname,
                Gender = a.gender,
                Age = a.age,
                Weight = a.weight,
                Color = a.color,
                Description = a.description,
                Photourl = a.photourl,
                Admissiondate = a.admissiondate,
                Status = a.status,
                Healthstatus = a.healthstatus
            };
        }
    }
}