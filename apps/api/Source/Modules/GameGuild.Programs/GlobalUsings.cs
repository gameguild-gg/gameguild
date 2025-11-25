// Global usings for Programs module
global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using FluentValidation;
global using GameGuild.Authentication.Enums;
global using GameGuild.CQRS;
global using GameGuild.Modules.Programs.Entities;
global using GameGuild.SharedKernel;
global using GameGuild.SharedKernel.Enums;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using ApplicationDbContext = GameGuild.API.Data.ApplicationDbContext;
