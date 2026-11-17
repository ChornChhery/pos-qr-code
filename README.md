Restaurant POS System - README.md
Here is a complete, professional README file for your project:
markdown
<div align="center">

# 🍽️ Restaurant POS & Ordering System

### A Modern Microservices-Based Point of Sale Solution for Small to Medium Restaurants

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-14+-000000?style=for-the-badge&logo=next.js&logoColor=white)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![TypeScript](https://img.shields.io/badge/TypeScript-Strict-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)

[Features](#-features) • [Architecture](#-architecture) • [Documentation](#-documentation) • [Quick Start](#-quick-start) • [Tech Stack](#-tech-stack)

</div>

---

## 📋 Overview

A **single-URL restaurant POS and ordering system** designed specifically for small to medium restaurants. The system serves two distinct user flows on the same domain:

1. **📱 Customer Flow:** Scan QR code → View menu → Customize orders → Place order (No login required)
2. **💼 Staff Flow:** Login → Unified dashboard for order management, kitchen display, and reporting

Built with **enterprise-grade microservices architecture** while maintaining simplicity for restaurant operations.

### 🎯 Key Business Benefits
- ✅ **Zero Customer Friction:** No app download, no account creation, just scan and order
- ✅ **Unified Owner/Kitchen View:** One dashboard for small teams (no context switching)
- ✅ **Real-Time Order Processing:** Kitchen sees orders instantly via WebSocket
- ✅ **Mobile-First Design:** Optimized for mid-range smartphones on restaurant Wi-Fi
- ✅ **Independent Service Updates:** Microservices allow updating one feature without touching others
- ✅ **Scalable Architecture:** Ready to grow from single location to multi-location

---

## ✨ Features

### For Customers (Public Interface)
- 📲 **QR Code Table Ordering:** Scan table QR → Instant menu access
- 🎨 **Beautiful Mobile Menu:** Fast-loading, image-rich, touch-optimized
- 🔧 **Product Customization:** Modifiers for spice levels, add-ons, special instructions
- 🛒 **Smart Cart:** Real-time price calculation with modifiers and taxes
- 📊 **Order Tracking:** Live status updates (Pending → Cooking → Ready)

### For Kitchen & Owner (Unified Dashboard)
- 👨‍🍳 **Live Order Queue (KDS):** Real-time order display with status management
- ⚡ **Quick Actions:** One-tap status updates (Start Cooking, Mark Ready)
- 📉 **Stock Management:** Toggle items "Sold Out" instantly (hides from customer menu)
- 🪑 **Table Management:** Track table status (Available, Occupied, Cleaning)
- 📈 **Basic Reports:** Daily sales, top-selling items, revenue breakdown

### For Cashiers
- 💰 **Payment Processing:** Cash and QR code payments
- 📸 **Slip Verification:** Upload and verify QR payment slips
- 🧾 **Receipt Printing:** Professional receipt generation
- 📋 **Order Management:** View and manage all active orders

### For Administrators
- 👥 **Staff Management:** Create users, assign roles (Owner, Admin, Cashier, Kitchen)
- 🍔 **Full Menu Control:** CRUD operations for categories, products, modifiers
- 📊 **Advanced Analytics:** Comprehensive sales reports, inventory tracking
- 🔒 **Audit Logging:** Track all sensitive actions (who did what, when)
- ⚙️ **System Configuration:** Tax rates, business hours, settings

---

## 🏗️ Architecture

### High-Level System Design
┌─────────────────────────────────────────────────────────────┐
│ CLIENTS │
│ ┌──────────────┐ ┌──────────────────────┐ │
│ │ Mobile Phone │ │ Staff Dashboard │ │
│ │ (Customer) │ │ (Owner/Kitchen) │ │
│ └──────┬───────┘ └──────────┬───────────┘ │
└─────────┼─────────────────────────────────┼────────────────┘
│ │
└────────────┬────────────────────┘
│
┌────────▼────────┐
│ API Gateway │
│ (YARP) │
│ Port: 5000 │
└────────┬────────┘
│
┌──────────────┼──────────────┬──────────────┐
│ │ │ │
┌───────▼──────┐ ┌─────▼──────┐ ┌────▼───────┐ ┌───▼──────────┐
│ Identity │ │ Catalog │ │ Order │ │ Notification │
│ Service │ │ Service │ │ Service │ │ Service │
│ Port: 5003 │ │ Port: 5001 │ │ Port: 5002 │ │ Port: 5005 │
└───────┬──────┘ └─────┬──────┘ └────┬───────┘ └───┬──────────┘
│ │ │ │
│ │ │ │
┌───────▼──────┐ ┌─────▼──────┐ ┌────▼───────┐ │
│ identity_db │ │ catalog_db │ │ order_db │ │
│ (PostgreSQL) │ │(PostgreSQL)│ │(PostgreSQL)│ │
└──────────────┘ └────────────┘ └────────────┘ │
│
┌────────────────────────────┘
│
┌────────▼────────┐
│ RabbitMQ │
│ (Event Bus) │
└─────────────────┘

### Microservices Breakdown

| Service | Purpose | Database | Port |
|---------|---------|----------|------|
| **API Gateway** | Single entry point, routing, rate limiting | N/A | 5000 |
| **Identity Service** | Authentication, JWT, RBAC, audit logs | `identity_db` | 5003 |
| **Catalog Service** | Menu, products, modifiers, stock | `catalog_db` | 5001 |
| **Order Service** | Tables, carts, orders, status tracking | `order_db` | 5002 |
| **Payment Service** | Cash/QR payments, slip verification | `payment_db` | 5004 |
| **Notification Service** | Real-time updates via SignalR | N/A | 5005 |

### Event-Driven Communication
Services communicate asynchronously via **RabbitMQ** for loose coupling:
- `OrderCreated` → Triggers stock deduction + kitchen notification
- `OrderStatusChanged` → Updates customer tracking
- `LowStockAlert` → Notifies managers
- `ProductUpdated` → Syncs across services

---

## 📚 Documentation

Comprehensive documentation is provided in separate markdown files:

| Document | Description |
|----------|-------------|
| [**PROJECT.md**](./PROJECT.md) | Business requirements, user flows, development phases |
| [**ARCHITECTURE.md**](./ARCHITECTURE.md) | Detailed technical architecture, microservices design |
| [**DEVELOPMENT.md**](./DEVELOPMENT.md) | Coding standards, git workflow, testing guidelines |
| [**API.md**](./API.md) | Complete API documentation with request/response examples |
| [**DATABASE.md**](./DATABASE.md) | Database schemas, relationships, migrations |
| [**CLAUDE.md**](./CLAUDE.md) | AI assistant instructions and patterns |
| [**SKILL.md**](./SKILL.md) | Required technical skills and competencies |
| [**UX_UI.md**](./UX_UI.md) | Design system, user flows, accessibility standards |
| [**SECURITY.md**](./SECURITY.md) | Threat model, security mitigations, compliance |
| [**TESTING_STRATEGY.md**](./TESTING_STRATEGY.md) | Testing pyramid, tools, quality gates |
| [**INFRASTRUCTURE.md**](./INFRASTRUCTURE.md) | DevOps strategy, cloud resources, CI/CD pipeline |
| [**ONBOARDING.md**](./ONBOARDING.md) | Quick start guide for new developers |

---

## 🚀 Quick Start

### Prerequisites
- **Docker Desktop** (for containerized development)
- **Node.js 20+** (for frontend)
- **.NET 10 SDK** (for backend services)
- **Git** (for version control)

### Installation Steps

```bash
# 1. Clone the repository
git clone <repository-url>
cd restaurant-pos

# 2. Copy environment variables
cp .env.example .env

# 3. Start infrastructure (PostgreSQL, RabbitMQ)
docker-compose up -d

# 4. Start backend services (in separate terminals)
cd services/ApiGateway && dotnet run
cd services/IdentityService && dotnet run
cd services/CatalogService && dotnet run
cd services/OrderService && dotnet run
cd services/NotificationService && dotnet run

# 5. Start frontend
cd apps/frontend
npm install
npm run dev
Access Points
Frontend: http://localhost:3000
API Gateway: http://localhost:5000
RabbitMQ Management: http://localhost:15672 (guest/guest)
PostgreSQL: localhost:5432 (pos_user/pos_password)
🛠️ Tech Stack
Frontend
Framework: Next.js 14+ (App Router)
Language: TypeScript (Strict Mode)
UI Library: React 18+ with Tailwind CSS
Components: Shadcn UI
State Management: Zustand (client), React Query (server)
Real-time: SignalR JavaScript client
Backend
Framework: .NET 10 with C# 12+
API Style: Minimal APIs (lightweight, fast)
Database ORM: Entity Framework Core
Messaging: MassTransit + RabbitMQ
Gateway: YARP (Yet Another Reverse Proxy)
Real-time: SignalR (built-in WebSocket support)
Database
Primary: PostgreSQL 16
Strategy: Database per microservice (no shared databases)
Features: JSONB for flexible data, UUID primary keys
Infrastructure
Containerization: Docker + Docker Compose
IaC: Terraform (for cloud provisioning)
CI/CD: GitHub Actions
Monitoring: OpenTelemetry + Prometheus + Grafana
🔒 Security
Authentication & Authorization
JWT Tokens: Short-lived access tokens (15 min) + long-lived refresh tokens (7 days)
Token Storage: Access token in memory, refresh token in HttpOnly Secure cookie
RBAC: Role-based access control (GUEST, CASHIER, KITCHEN, MANAGER, ADMIN)
Validation: JWT validated at API Gateway AND sensitive microservices
Security Features
✅ Password hashing (bcrypt/Argon2)
✅ SQL injection prevention (parameterized queries)
✅ XSS protection (React escaping + CSP headers)
✅ CORS configuration
✅ Rate limiting on API Gateway
✅ Audit logging for sensitive actions
✅ HTTPS enforcement in production
See SECURITY.md for detailed threat model and mitigations.
📊 Development Phases
Phase 1: Foundation & Infrastructure (Week 1-2)
Repository setup and Docker Compose
API Gateway (YARP) configuration
Identity Service (JWT auth, RBAC)
CI/CD pipeline setup
Phase 2: Core Business Services (Week 3-4)
Catalog Service (products, modifiers, stock)
Order Service (cart, order lifecycle)
Event-driven communication (RabbitMQ)
Real-time updates (SignalR)
Phase 3: Frontend - Public Menu (Week 5)
Next.js app structure
Mobile-optimized menu (SSR)
Cart functionality
Order submission
Phase 4: Frontend - Staff Dashboard (Week 6)
Staff login system
Unified Owner/Kitchen dashboard
Real-time order queue
Menu and table management
Phase 5: Payment & Advanced Features (Week 7)
Payment Service (cash, QR, slip upload)
Admin dashboard (reports, staff management)
Audit logging
Receipt printing
Phase 6: Testing, DevOps & Deployment (Week 8-9)
Unit and integration tests
Load testing and optimization
Terraform infrastructure
Production deployment
Monitoring setup
Total Timeline: 8-9 weeks (10-11 weeks with buffer)
🧪 Testing
Testing Strategy
70% Unit Tests: Business logic (price calculation, validation)
20% Integration Tests: Service + database + message broker
10% E2E Tests: Critical user journeys (Playwright)
Quality Gates
✅ 80%+ code coverage required
✅ All E2E tests must pass
✅ No linting errors
✅ Security audit passed
See TESTING_STRATEGY.md for detailed testing guidelines.
🌐 Deployment
Environments
Local Development: Docker Compose with hot-reload
Staging: Production mirror for QA and UAT
Production: High-availability, auto-scaling, monitored
Cloud Resources (AWS Example)
Compute: AWS ECS Fargate (serverless containers)
Database: Amazon RDS PostgreSQL (Multi-AZ)
Message Broker: Amazon MQ (Managed RabbitMQ)
Storage: Amazon S3 (images, payment slips)
CDN: Cloudflare (caching, DDoS protection)
CI/CD Pipeline
Push to main → Run tests → Build Docker images
Tag release → Deploy to Staging → Run E2E tests
If passed → Deploy to Production (Blue/Green)
See INFRASTRUCTURE.md for detailed DevOps strategy.
🤝 Contributing
Development Workflow
Create feature branch: feature/POS-123-add-modifier-support
Make changes and commit (follow Conventional Commits)
Push and create Pull Request
Request review from team
Address feedback and squash merge
Commit Message Format
<type>(<scope>): <subject>

feat(order): add support for order notes
fix(catalog): correct stock deduction logic
docs(api): update payment endpoint documentation
Code Review Checklist
Code follows style guidelines
Tests added/updated
Documentation updated
No security vulnerabilities
Performance considered
See DEVELOPMENT.md for complete guidelines.
📸 Screenshots
Screenshots will be added once the UI is implemented
Customer Menu (Mobile)
<!-- Add screenshot here -->

Kitchen Display System
<!-- Add screenshot here -->

Admin Dashboard
<!-- Add screenshot here -->

🐛 Troubleshooting
Common Issues
Services can't connect to PostgreSQL
Check docker-compose.yml network configuration
Ensure services are on the same network
Verify connection strings in .env
RabbitMQ messages not being consumed
Check RabbitMQ Management UI (http://localhost:15672)
Verify queues and bindings are correct
Check service logs for connection errors
Frontend can't connect to API
Verify NEXT_PUBLIC_API_URL in .env.local
Check CORS settings in API Gateway
Ensure API Gateway is running on port 5000
JWT token validation fails
Verify JWT secret is same across all services
Check token expiry (15 min for access, 7 days for refresh)
Ensure issuer and audience match configuration
See DEVELOPMENT.md for more troubleshooting tips.
📝 License
This project is proprietary software. All rights reserved.