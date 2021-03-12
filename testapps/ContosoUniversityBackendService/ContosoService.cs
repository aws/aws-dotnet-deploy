using ContosoUniversityBackendService.Data;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversityBackendService
{
    public class ContosoService : BackgroundService
    {
        SchoolContext Context { get; }
        public ContosoService(SchoolContext context)
        {
            Context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Student List");
                Console.WriteLine("------------");
                foreach(var student in Context.Students)
                {
                    Console.WriteLine(student.FirstMidName + student.LastName);
                }

                Console.WriteLine(string.Empty);
                await Task.Delay(1000);
            }
        }
    }
}
