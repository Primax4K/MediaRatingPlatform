# Project Protocol

## Application Design, Architecture & Decisions

Das Projekt ist als leichtgewichtige HTTP-Web-API mit `HttpListener` umgesetzt und verzichtet bewusst auf ASP.NET Core. Diese Entscheidung ermöglicht volle Kontrolle über Routing, Authentifizierung und Fehlerbehandlung und reduziert Framework-Abhängigkeiten.

Die Anwendung folgt einer klaren **Layered Architecture**:
- **WebAPI**
  - Routing, HTTP-Verarbeitung, Authentifizierung
  - DTOs für Ein- und Ausgabe
- **Domain**
  - Repository-Interfaces
  - Geschäftslogik und Validierungsregeln
- **Model**
  - Zentrale Entitäten wie User, Media, Rating, Favorite

Das Routing ist selbst implementiert und unterstützt sowohl statische als auch parametrisierte Pfade. Jede Route definiert explizit, ob Authentifizierung erforderlich ist. Die zentrale Weiterleitung erfolgt über einen Router-Handler.

Die Authentifizierung basiert auf JWT. Token-Erstellung und -Validierung sind zentral gekapselt, um Duplikate zu vermeiden. Autorisierung wird auf Routenebene geprüft.

Der Datenzugriff erfolgt über das Repository-Pattern mit PostgreSQL und bewusstem Einsatz von Raw-SQL. Dies erhöht Transparenz, Kontrolle und Performance. Gemeinsame Datenbank-Hilfsklassen reduzieren Wiederholungen.

Abhängigkeiten werden über `Microsoft.Extensions.DependencyInjection` verwaltet, wodurch klare Trennung zwischen Abstraktion und Implementierung erreicht wird.

---

## Unit-Testing-Strategie und Abdeckung

Die Unit-Tests fokussieren sich auf **relevante Geschäftslogik und Endpunkt-Verhalten**, basierend auf `AuthHandlerTests` und `EndpointTests_10_Total`.

**Getestet werden:**
- **Authentifizierung**
  - Registrierung (Erfolg / Konflikt)
  - Passwort-Hashing und Login-Validierung
  - JWT-Erstellung, -Validierung und Auslesen der UserId
- **Endpoints (User, Media, Rating)**
  - Input-Validierung (ungültige Daten, ungültige GUIDs)
  - Fehlerfälle (`400`, `401`, `403`, `404`, `409`)
  - Business-Regeln wie Ownership-Prüfungen und Duplikat-Schutz
  - Korrekte HTTP-Statuscodes

**Nicht getestet** werden DTOs, triviale Getter/Setter sowie interne Details von `HttpListener`, um die Tests **einfach, zielgerichtet und wartbar** zu halten.

---

## SOLID-Prinzipien

**Single Responsibility Principle (SRP)**  
Jede Klasse erfüllt genau eine Aufgabe.  
Beispiele:
- `AuthHandler` ist ausschließlich für Authentifizierung zuständig
- Repository-Klassen enthalten nur Datenbanklogik

**Dependency Inversion Principle (DIP)**  
Handler arbeiten ausschließlich gegen Interfaces.  
Beispiel:
- Abhängigkeit zu `IUserRepository` statt konkreter Implementierung  
  → Erleichtert Mocking und Unit-Tests

**Open/Closed Principle (OCP)**  
Neue Routen oder Repository-Implementierungen können ergänzt werden, ohne bestehende Logik zu verändern.

---

## Probleme & Lösungen

- **Komplexität durch manuelles Routing**  
  → Lösung durch wiederverwendbare Pfad-Normalisierung und Pattern-Matching

- **Duplizierte JWT-Logik**  
  → Zentralisierung in Auth-Handler und HTTP-Extensions

- **SQL-Mapping-Fehler**  
  → Eindeutige Spalten-Aliase und konsistente Mapping-Helfer

- **Blockierender Server bei Tests**  
  → Tests auf Handler- und Repository-Ebene ohne laufenden Server

---

## Lessons Learned

## Lessons Learned (Technical)

- **Business-Logik gehört nicht in Router oder HTTP-Code**  
  Logik in Routern führte zu doppeltem Code und schlechter Testbarkeit. Das Auslagern in Handler und Repositories machte den Code strukturierter und wartbarer.

- **Repository-Interfaces sind entscheidend für saubere Tests**  
  Ohne Interfaces wären Unit-Tests nur mit echter Datenbank möglich gewesen. Durch klare Abstraktionen konnte die Datenzugriffsschicht zuverlässig gemockt werden.

- **Manuelles Routing braucht strikte Konventionen**  
  Kleine Abweichungen bei Pfaden verursachten Fehler. Einheitliche Normalisierung und zentrales Pattern-Matching waren notwendig für stabiles Routing.

- **Raw-SQL ist transparent, aber brutal**  
  Fehler in Spaltennamen oder fehlende Aliase führten direkt zu Laufzeitfehlern. Saubere Queries und konsistentes Mapping sind zwingend erforderlich.


---

## Time Tracking (~35h)

| Task                                   | Time (h) |
|----------------------------------------|----------|
| Project setup & configuration          | 3.0      |
| Database schema & SQL queries           | 4.0      |
| Repository implementation              | 6.0      |
| Custom routing system                  | 4.0      |
| Authentication (JWT + BCrypt)          | 3.0      |
| Media, Rating, Favorite logic          | 5.0      |
| Error handling & validation            | 3.0      |
| Unit test design & implementation      | 5.0      |
| Debugging & refactoring                | 2.0      |
| **Total**                              | **35.0** |

---

## Git Repository

Die Git-History dokumentiert die schrittweise Entwicklung, Refactorings und Bugfixes und ist Teil der Projektdokumentation.

https://github.com/Primax4K/MediaRatingPlatform
