using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Dapper;

public record Empleado(int Id, string Nombre, string Cargo, string Titulo, bool EsPlanta);
public record Relacion(string PersonaA, string PersonaB, string Vinculo);

class Program
{
    static void Main()
    {
        // 🔐 CONNECTION STRING (Azure SQL)
        string connectionString =
            "Server=tcp:server-lpo-jullians-01.database.windows.net,1433;" +
            "Initial Catalog=TallerLPO;" +
            "Persist Security Info=False;" +
            "User ID=adminlpo;" +        // 👈 CAMBIA si tu usuario es otro
            "Password=Admin123;" +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout=30;";

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            Console.WriteLine("✅ Conectado a Azure SQL correctamente.\n");

            // --- BASE DE CONOCIMIENTO DESDE AZURE ---
            var empleados = connection
                .Query<Empleado>("SELECT Id, Nombre, Cargo, Titulo, EsPlanta FROM Empleados;")
                .ToList();

            var parentescos = connection
                .Query<Relacion>("SELECT PersonaA, PersonaB, Vinculo FROM Relaciones;")
                .ToList();

            // --- MOTOR DE INFERENCIA ---

            // 1️⃣ Cuantificador Universal
            bool todosCumplen = empleados
                .Where(e => e.EsPlanta)
                .All(e => e.Titulo == "Magister");

            Console.WriteLine($"¿Todos los docentes de planta tienen Maestría?: {todosCumplen}");

            // 2️⃣ Conflictos de parentesco (Nepotismo)
            var conflictos = from e1 in empleados
                             from e2 in empleados
                             join p in parentescos
                             on new { A = e1.Nombre, B = e2.Nombre }
                             equals new { A = p.PersonaA, B = p.PersonaB }
                             where p.Vinculo == "Hermano"
                             select new { Nombre1 = e1.Nombre, Nombre2 = e2.Nombre };

            Console.WriteLine("\nConflictos de Nepotismo detectados:");
            foreach (var c in conflictos)
            {
                Console.WriteLine($"- Alerta: {c.Nombre1} y {c.Nombre2} son familiares en la misma nómina.");
            }

            // 3️⃣ Regla de Inferencia de Nómina (CalcularBono(x,y))

            Func<Empleado, double> CalcularBono = e =>
            {
                if (e.Cargo == "Profesor" && e.Titulo == "Magister")
                    return 0.15;

                if (e.Titulo == "Especialista")
                    return 0.10;

                return 0.0;
            };

            Console.WriteLine("\nCálculo de Bonos:");
            foreach (var e in empleados)
            {
                double bono = CalcularBono(e);
                Console.WriteLine($"{e.Nombre} recibe un bono del {bono * 100}%");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error conectando a Azure SQL:");
            Console.WriteLine(ex.Message);
        }
    }
}