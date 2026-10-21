# ASP.NET MVC Security Project

This project is developed for the **Software and Database Security** course using the **ASP.NET MVC** framework. It focuses on implementing robust security mechanisms, secure coding practices, and data protection standards.

##  Key Security Features

* **Authentication & Session Management:** Secure user login, password hashing, and session protection.
* **Access Control (RBAC/Authorization):** Role-based access control to protect sensitive controllers and actions.
* **OWASP Top 10 Prevention:** 
  * Protection against SQL Injection (using Entity Framework LINQ parameterization).
  * CSRF defense via Anti-Forgery Tokens.
  * XSS mitigation and secure output encoding.
* **Database Security:** Secure connection strings, data sanitization, and least-privilege database access.

##  Tech Stack

* **Framework:** ASP.NET MVC (.NET)
* **Database:** SQL Server / Entity Framework Core
* **Tools:** Visual Studio / VS Code, Git

##  Project Structure

```text
aspnet-mvc-security/
│
├── Controllers/       # Request handlers & security filters
├── Models/            # Data models & business logic
├── Views/             # Razor views with security tokens
├── Data/              # Database context & migrations
└── README.md
