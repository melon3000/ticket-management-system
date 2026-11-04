using Microsoft.AspNetCore.Identity;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Data;

/// <summary>Fills an empty database with starting data for development and demonstration.</summary>
public static class SeedData
{
    /// <summary>Password of every seeded account. For development only.</summary>
    public const string DevPassword = "Password123!";

    public const string AdminEmail = "admin@ticketsystem.local";
    public const string Support1Email = "mari.maasikas@ticketsystem.local";
    public const string Support2Email = "jaan.tamm@ticketsystem.local";
    public const string User1Email = "kati.kask@ticketsystem.local";
    public const string User2Email = "toomas.mets@ticketsystem.local";
    public const string ServiceEmail = "service@ticketsystem.local";

    /// <summary>Adds the starting data. Does nothing when users already exist, so it is safe to call on every start.</summary>
    public static void Initialize(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hasher = new PasswordHasher<User>();

        User NewUser(string email, string name, UserRole role)
        {
            var user = new User { Email = email, DisplayName = name, Role = role, CreatedAt = now.AddDays(-30) };
            user.PasswordHash = hasher.HashPassword(user, DevPassword);
            return user;
        }

        var admin = NewUser(AdminEmail, "Peeter Admin", UserRole.Admin);
        var support1 = NewUser(Support1Email, "Mari Maasikas", UserRole.Support);
        var support2 = NewUser(Support2Email, "Jaan Tamm", UserRole.Support);
        var user1 = NewUser(User1Email, "Kati Kask", UserRole.User);
        var user2 = NewUser(User2Email, "Toomas Mets", UserRole.User);
        var service = NewUser(ServiceEmail, "Service Account", UserRole.Service);

        var hardware = new Category { Name = "Riistvara" };
        var software = new Category { Name = "Tarkvara" };
        var network = new Category { Name = "Võrk" };
        var accounts = new Category { Name = "Kontod ja ligipääs" };
        var other = new Category { Name = "Muu" };

        var low = new Priority { Name = "Madal", Level = 1, DeadlineHours = 72 };
        var medium = new Priority { Name = "Keskmine", Level = 2, DeadlineHours = 24 };
        var high = new Priority { Name = "Kõrge", Level = 3, DeadlineHours = 8 };
        var critical = new Priority { Name = "Kriitiline", Level = 4, DeadlineHours = 4 };

        // Builds a ticket together with the change log entries that its history would have produced.
        Ticket NewTicket(string title, string description, Category category, Priority priority, User author,
            DateTime createdAt, TicketStatus status, User? assignee = null, double? resolvedAfterHours = null)
        {
            var ticket = new Ticket
            {
                Title = title,
                Description = description,
                Category = category,
                Priority = priority,
                CreatedBy = author,
                Assignee = assignee,
                Status = status,
                CreatedAt = createdAt,
                Deadline = createdAt.AddHours(priority.DeadlineHours),
                ResolvedAt = resolvedAfterHours.HasValue ? createdAt.AddHours(resolvedAfterHours.Value) : null
            };

            ticket.Logs.Add(new TicketLog
            {
                User = author, Action = TicketLogActions.Created, NewValue = nameof(TicketStatus.New), CreatedAt = createdAt
            });

            if (assignee != null)
            {
                ticket.Logs.Add(new TicketLog
                {
                    User = assignee, Action = TicketLogActions.Assigned, NewValue = assignee.DisplayName,
                    CreatedAt = createdAt.AddMinutes(30)
                });
                ticket.Logs.Add(new TicketLog
                {
                    User = assignee, Action = TicketLogActions.StatusChanged, OldValue = nameof(TicketStatus.New),
                    NewValue = nameof(TicketStatus.InProgress), CreatedAt = createdAt.AddMinutes(31)
                });
            }

            if (ticket.ResolvedAt.HasValue && assignee != null)
            {
                ticket.Logs.Add(new TicketLog
                {
                    User = assignee, Action = TicketLogActions.StatusChanged, OldValue = nameof(TicketStatus.InProgress),
                    NewValue = nameof(TicketStatus.Resolved), CreatedAt = ticket.ResolvedAt.Value
                });
            }

            return ticket;
        }

        var tickets = new List<Ticket>
        {
            // Open tickets that are still on time.
            NewTicket("Printer ei tööta", "Teise korruse printer näitab veateadet ega prindi.",
                hardware, high, user1, now.AddHours(-1), TicketStatus.New),
            NewTicket("Wi-Fi ühendus katkeb", "Koosolekuruumis katkeb Wi-Fi iga kümne minuti järel.",
                network, medium, user2, now.AddHours(-3), TicketStatus.InProgress, support1),
            NewTicket("Ei saa süsteemi sisse logida", "Parool ei kehti pärast puhkust, konto näib lukus olevat.",
                accounts, critical, user1, now.AddHours(-1), TicketStatus.InProgress, support2),
            NewTicket("Excel jookseb kokku", "Suure tabeli avamisel Excel sulgub ilma veateateta.",
                software, low, user2, now.AddHours(-20), TicketStatus.New),

            // Resolved tickets, used by the resolution time statistics.
            NewTicket("Uue töötaja konto loomine", "Uuele töötajale on vaja e-posti ja süsteemi kontod.",
                accounts, medium, user1, now.AddDays(-4), TicketStatus.Resolved, support1, 5),
            NewTicket("Monitor on must", "Töölaua monitor ei näita pilti, toitetuli põleb.",
                hardware, high, user2, now.AddDays(-3), TicketStatus.Resolved, support2, 6),
            NewTicket("VPN ei ühendu", "Kodust ei õnnestu ettevõtte VPN-iga ühendada.",
                network, high, user1, now.AddDays(-6), TicketStatus.Resolved, support1, 3),
            NewTicket("Outlook ei sünkroniseeri", "Uued kirjad ilmuvad postkasti mitme tunni hilinemisega.",
                software, medium, user2, now.AddDays(-5), TicketStatus.Resolved, support2, 20),
            NewTicket("Server ei vasta", "Failiserver on kättesaamatu, meeskond ei pääse dokumentidele ligi.",
                network, critical, user1, now.AddDays(-2), TicketStatus.Resolved, support1, 2),
            NewTicket("Projektori kaabel puudub", "Koosolekuruumist on kadunud projektori HDMI-kaabel.",
                other, low, user2, now.AddDays(-7), TicketStatus.Resolved, support2, 30),

            // Late ticket: its deadline has passed, so the Worker should find it and escalate it.
            NewTicket("Tulemüür blokeerib rakenduse", "Raamatupidamisprogramm ei ühendu serveriga, kahtlustatav on tulemüür.",
                network, high, user1, now.AddDays(-3), TicketStatus.InProgress, support2)
        };

        // A few comments so that the conversation view has something to show.
        tickets[1].Comments.Add(new Comment { Author = support1, Text = "Vaatan ruumi ruuteri üle.", CreatedAt = now.AddHours(-2) });
        tickets[1].Comments.Add(new Comment { Author = user2, Text = "Aitäh, katkeb endiselt.", CreatedAt = now.AddHours(-1) });
        tickets[2].Comments.Add(new Comment { Author = support2, Text = "Lähtestan parooli, saadan uue ajutise.", CreatedAt = now.AddMinutes(-30) });
        tickets[10].Comments.Add(new Comment { Author = support2, Text = "Küsisin IT-halduselt tulemüüri reeglite kohta.", CreatedAt = now.AddDays(-2) });

        db.Users.AddRange(admin, support1, support2, user1, user2, service);
        db.Categories.AddRange(hardware, software, network, accounts, other);
        db.Priorities.AddRange(low, medium, high, critical);
        db.Tickets.AddRange(tickets);
        db.SaveChanges();
    }
}
