# 🛠️ TooliRent

TooliRent är ett uthyrningssystem för verktyg byggt på **.NET 8** och **ASP.NET Core Web API**.  
Systemet hanterar medlemmar, reservationer, lån och administration – med stöd för batch-hantering, tillgänglighetskontroller och statistik.

---

## 🏗️ Arkitektur

Projektet är organiserat enligt **Clean Architecture**:

- **Core**  
  Innehåller domänmodeller (`Tool`, `Member`, `Reservation`, `Loan`), enums och interfaces.

- **Infrastructure**  
  Databasåtkomst med **Entity Framework Core**.  
  Innehåller `TooliRentDbContext`, repository-implementationer och migrations.

- **Services**  
  Affärslogik och validering.  
  - DTOs (Data Transfer Objects)  
  - Services (t.ex. `ReservationService`, `LoanService`, `AdminService`)  
  - Validators (FluentValidation)  
  - Mapping-profiler (AutoMapper)

- **WebAPI**  
  Controllers som exponerar REST-endpoints.  
  - Autentisering via JWT  
  - Auktorisering med roller: **Member** och **Admin**  
  - Standardiserad felhantering med `ProblemDetails`

---

📚 API Endpoints

👤 Medlemmar (Admin)
	•	GET /api/admin/members – Lista/sök medlemmar
	•	GET /api/admin/members/{id} – Hämta medlem
	•	PATCH /api/admin/members/{id}/status – Ändra status

📅 Reservationer
	•	POST /api/me/reservations – Skapa reservation (member)
	•	GET /api/me/reservations/{id} – Hämta reservation (member)
	•	GET /api/me/reservations/active – Lista aktiva reservationer (member)
	•	POST /api/me/reservations/{id}/cancel – Avbryt reservation (member)
	•	POST /api/admin/reservations/batch – Skapa batch-reservation (admin)

📦 Lån
	•	GET /api/loans/my – Lista egna lån (member)
	•	GET /api/loans/{id} – Hämta lån (member)
	•	POST /api/loans/my/checkout-batch – Checkout via reservation eller direkt (member)
	•	POST /api/loans/my/{id}/return – Returnera lån (member)
	•	POST /api/admin/loans/checkout-batch – Checkout batch (admin)
	•	POST /api/admin/loans/{id}/return – Returnera lån (admin)

📊 Statistik (Admin)
	•	GET /api/admin/stats – Hämta statistik (lån, intäkter, populära verktyg, kategorier)


