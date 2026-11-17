The Complete AI Context Package:
restaurant-pos/
├── README.md              ← Entry point (overview + links)
├── PROJECT.md             ← Business requirements & phases
├── ARCHITECTURE.md        ← Technical architecture details
├── DEVELOPMENT.md         ← Coding standards & git workflow
├── API.md                 ← Complete API documentation
├── DATABASE.md            ← Database schemas
├── CLAUDE.md              ← AI-specific instructions ⭐ MOST IMPORTANT
├── SKILL.md               ← Required competencies
├── UX_UI.md               ← Design guidelines
├── SECURITY.md            ← Security strategy
├── TESTING_STRATEGY.md    ← Testing approach
├── INFRASTRUCTURE.md      ← DevOps & deployment
└── ONBOARDING.md          ← Quick start guide
How to Use These Files with AI
Option 1: Feed All Files to AI (Recommended)
When you start a new AI chat session, tell the AI:
"Read all the markdown files in the root directory to understand the project. Start with CLAUDE.md, then read PROJECT.md, ARCHITECTURE.md, and the others. Once you understand the full context, I'll ask you to help with [specific task]."
The AI will then read all 12 files and have complete context.
Option 2: Use CLAUDE.md as the Master File
The CLAUDE.md file is specifically designed for AI assistants. It contains:
Project summary
Strict rules and constraints
Common patterns and code examples
Troubleshooting guides
You can tell the AI:
"Read CLAUDE.md first. This file contains all the rules and patterns you need to follow. Then read the other markdown files for detailed specifications."
Option 3: Create a Single "Mega Context" File (If You Prefer)
If you want one single file that contains everything (so you don't have to manage 12 files), I can create a FULL_PROJECT_CONTEXT.md that combines all the content from the 12 files into one document.
Would you like me to create this single mega-file? It would be very long (probably 2000+ lines), but it would give an AI complete context in one read.
My Recommendation
Keep the 12 separate files because:
Easier to maintain - When you update the API, you only edit API.md, not a 2000-line mega file
Better organization - Each file has a clear purpose
AI can read selectively - If you're working on frontend, the AI only needs to read UX_UI.md and API.md, not the database schema
Professional structure - This is how real enterprise projects are documented
The README.md serves as the "table of contents" that tells both humans and AI: "Here's what this project is about, and here are all the detailed docs you need to read."
Quick Answer
Can the README alone help an AI understand your project?
✅ Yes - for a high-level overview
❌ No - for actually building features, writing code, or making architectural decisions
What should you do?
Keep the README as the entry point
When working with AI, tell it to read CLAUDE.md first, then the other relevant files
Or, if you prefer, I can create one mega-file that combines everything
Which approach do you prefer?
Keep 12 separate files (recommended)
Create one mega FULL_PROJECT_CONTEXT.md file
so give me everything that i can use that for ai and understand and do everything for me

Thinking completed
Complete AI Context File - Restaurant POS System
Here is a single, comprehensive markdown file that contains ALL the information an AI needs to understand your entire project and help you build it. Save this as AI_CONTEXT.md in your repository root.
markdown
# 🍽️ Restaurant POS System - Complete AI Context

> **IMPORTANT:** This file contains the complete context for the Restaurant POS project. Read this entire file before making any suggestions, writing code, or providing guidance.

---

## 📑 Table of Contents

1. [Project Overview](#1-project-overview)
2. [Business Requirements](#2-business-requirements)
3. [Technical Architecture](#3-technical-architecture)
4. [Microservices Design](#4-microservices-design)
5. [Database Schemas](#5-database-schemas)
6. [API Documentation](#6-api-documentation)
7. [Frontend Guidelines](#7-frontend-guidelines)
8. [Backend Guidelines](#8-backend-guidelines)
9. [Security Strategy](#9-security-strategy)
10. [Testing Strategy](#10-testing-strategy)
11. [DevOps & Infrastructure](#11-devops--infrastructure)
12. [Development Phases](#12-development-phases)
13. [AI Assistant Rules](#13-ai-assistant-rules)
14. [Common Patterns](#14-common-patterns)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. Project Overview

### What is this project?
A **single-URL restaurant POS and ordering system** for small to medium restaurants. The system serves two distinct user flows on the same domain:

1. **Customer Flow (Public):** Scan QR code → View menu → Customize orders → Place order (No login required)
2. **Staff Flow (Private):** Login → Unified dashboard for order management, kitchen display, and reporting

### Key Business Benefits
- Zero customer friction (no app download, no account creation)
- Unified Owner/Kitchen view (one dashboard for small teams)
- Real-time order processing via WebSocket
- Mobile-first design optimized for mid-range smartphones
- Independent service updates via microservices
- Scalable from single location to multi-location

### Target Users
1. **Customers:** Scan QR, browse menu, order (no account needed)
2. **Owner/Kitchen:** Unified dashboard for order management and menu control
3. **Cashier:** Process payments (cash/QR), print receipts
4. **Admin:** Full system control (staff, reports, settings, audit logs)

### Success Metrics
- Order placement to kitchen display: < 1 second
- Mobile menu load time: < 2 seconds on 3G
- System uptime: 99.9%
- Zero data loss during order processing

---

## 2. Business Requirements

### Customer Flow (No Login Required)

**Entry Point:** Customer scans QR code on table → URL: `yourdomain.com/?table=12`

**Workflow:**
1. Scan QR → Instantly see menu (no loading spinner, use skeleton loaders)
2. Tap Category → Smooth scroll to section
3. Tap Product → Modal slides up from bottom showing image, description, price, and Modifiers
4. Select Modifiers → "Add to Cart" button becomes active and shows total price
5. View Cart → Clear breakdown of items, modifiers, subtotal, tax, and total
6. Confirm Order → Success animation → "Kitchen is preparing your order" screen

**Key Features:**
- Mobile-optimized menu with categories and products
- Product customization (e.g., Papaya Salad → Spiciness: Mild/Medium/Hot, Add-ons: Crab +$2)
- Cart management with real-time price calculation
- Order submission to kitchen/cashier in real-time
- No account required - customers just scan and order

### Owner/Kitchen Flow (Unified Dashboard)

**Single Login:** Owner logs into one dashboard

**Default View - Live Orders Queue (KDS):**
- Shows incoming orders in real-time
- Status transitions: `Pending` → `Cooking` → `Ready`
- Big buttons for quick status updates
- Each order card shows: Table Number (Large), Time elapsed (turns red after 15 mins), Items (with modifiers highlighted)
- Action: Tap "Start Cooking" → Card changes color, moves to next column
- Action: Tap "Ready" → Card disappears from KDS, triggers notification

**Menu Management:**
- Quick toggles to mark items "Sold Out" (hides from customer menu instantly)
- View all products, categories, modifiers
- Update prices, descriptions, images

**Table Management:**
- Mark tables as Available/Occupied/Cleaning
- View QR codes for each table

**Reports:**
- End-of-day sales
- Top-selling items
- Revenue breakdown

### Cashier Flow

**Order Queue:**
- See all orders with prices
- Process payments

**Payment Processing:**
- Cash payment: Mark as paid, calculate change
- QR payment: Generate QR code for customer OR customer uploads payment slip photo → Cashier verifies and completes
- Receipt printing for customers

**Workflow:**
1. Dashboard shows "Pending Payments" list
2. Tap Order → Shows total and payment method
3. If QR: Show "Awaiting Customer Upload" or "Scan to Pay" QR
4. If Slip Uploaded: Show thumbnail of slip → Cashier taps "Verify & Complete"
5. Success → Option to "Print Receipt" appears

### Admin Flow (Full System Control)

**Dashboard:**
- Analytics, charts, key metrics

**Staff Management:**
- Create users, assign roles (Owner, Admin, Cashier, Kitchen)
- View audit logs

**Table Management:**
- Add/edit/delete tables
- Generate QR codes

**Menu Management:**
- Full CRUD for categories, products, modifiers

**Order Management:**
- View all orders
- Handle disputes/voids

**Reports:**
- Comprehensive sales reports
- Inventory reports

**Settings:**
- System configuration
- Tax rates
- Business hours

---

## 3. Technical Architecture

### Tech Stack

**Frontend:**
- Framework: Next.js 14+ (App Router)
- Language: TypeScript (Strict Mode)
- UI Library: React 18+ with Tailwind CSS
- Components: Shadcn UI
- State Management: Zustand (client state), React Query (server state)
- Real-time: SignalR JavaScript client or native WebSockets
- Rendering: SSR for public menu (fast mobile loads), CSR for staff dashboard

**Backend:**
- Framework: .NET 10 with C# 12+
- API Style: Minimal APIs (lightweight, fast)
- Database ORM: Entity Framework Core
- Messaging: MassTransit + RabbitMQ
- Gateway: YARP (Yet Another Reverse Proxy)
- Real-time: SignalR (built-in WebSocket support)
- Architecture: Microservices with Event-Driven Communication

**Database:**
- Primary: PostgreSQL 16
- Strategy: Database per microservice (no shared databases)
- Features: JSONB for flexible data, UUID primary keys

**Infrastructure:**
- Containerization: Docker + Docker Compose
- IaC: Terraform (for cloud provisioning)
- CI/CD: GitHub Actions
- Monitoring: OpenTelemetry + Prometheus + Grafana

### High-Level Architecture Diagram
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

### Architecture Pattern
**Microservices with Event-Driven Communication**
- Each service has its own database
- Services communicate asynchronously via RabbitMQ events
- API Gateway is the single entry point for all client requests
- Real-time updates via SignalR/WebSockets

---

## 4. Microservices Design

### Service 1: API Gateway (YARP)
**Purpose:** Single entry point for all client requests

**Responsibilities:**
- Route requests to appropriate microservice
- Rate limiting and throttling
- CORS handling
- Initial JWT validation
- Request/response logging
- Load balancing

**Routing Rules:**
/api/auth/* → Identity Service
/api/catalog/* → Catalog Service
/api/orders/* → Order Service
/api/payments/* → Payment Service
/hubs/* → Notification Service (SignalR)

**Port:** 5000

### Service 2: Identity Service
**Purpose:** Authentication and authorization

**Responsibilities:**
- User registration and login
- JWT token generation (access + refresh)
- Role management (GUEST, CASHIER, KITCHEN, MANAGER, ADMIN)
- Password hashing (bcrypt)
- Audit logging (login attempts, password changes)

**Database:** `identity_db`

**Key Endpoints:**
POST /api/auth/login
POST /api/auth/register
POST /api/auth/refresh
GET /api/auth/users (ADMIN only)
PUT /api/auth/users/{id}/role (ADMIN only)

**Port:** 5003

### Service 3: Catalog Service
**Purpose:** Menu and inventory management

**Responsibilities:**
- Categories CRUD
- Products CRUD
- Modifiers/Options management
- Stock tracking
- "Sold Out" status toggling
- Image upload to cloud storage

**Database:** `catalog_db`

**Events Published:**
- `ProductCreated`
- `ProductUpdated`
- `StockChanged` (triggers inventory updates in other services)

**Key Endpoints:**
GET /api/catalog/categories
GET /api/catalog/products
GET /api/catalog/products/{id}
POST /api/catalog/products (ADMIN/MANAGER only)
PUT /api/catalog/products/{id} (ADMIN/MANAGER only)
PATCH /api/catalog/products/{id}/sold-out (ADMIN/MANAGER only)

**Port:** 5001

### Service 4: Order Service
**Purpose:** Order lifecycle management

**Responsibilities:**
- Table session management (multiple customers, one table)
- Cart management (temporary storage)
- Order creation and validation
- Order status transitions (PENDING → COOKING → READY → COMPLETED)
- Price calculation (including modifiers)
- Stock availability check (via Catalog Service)

**Database:** `order_db`

**Events Published:**
- `OrderCreated` (triggers stock deduction and kitchen notification)
- `OrderStatusChanged` (triggers customer notification)

**Events Consumed:**
- `StockChanged` (to check product availability)

**Key Endpoints:**
GET /api/orders/tables
POST /api/orders/cart (add item to cart)
POST /api/orders/checkout (create order from cart)
GET /api/orders (staff only - view all orders)
GET /api/orders/{id}
PATCH /api/orders/{id}/status (staff only - update status)

**Port:** 5002

### Service 5: Payment Service
**Purpose:** Payment processing

**Responsibilities:**
- Cash payment recording
- QR code generation (if integrated with payment gateway)
- Payment slip photo upload (to cloud storage)
- Payment verification (cashier confirms receipt)
- Refund processing

**Database:** `payment_db`

**Events Published:**
- `PaymentCompleted` (triggers order completion)

**Key Endpoints:**
POST /api/payments/cash
POST /api/payments/qr
POST /api/payments/upload-slip
POST /api/payments/{id}/verify (CASHIER/MANAGER only)

**Port:** 5004

### Service 6: Notification Service
**Purpose:** Real-time communication

**Responsibilities:**
- SignalR hub management
- Push live order updates to Kitchen/Cashier dashboards
- Send notifications for low stock, new orders, status changes
- Manage WebSocket connections

**Events Consumed:**
- `OrderCreated`
- `OrderStatusChanged`
- `LowStockAlert`

**SignalR Hubs:**
- `/hubs/kitchen` - Kitchen staff receive order updates
- `/hubs/cashier` - Cashier receives payment notifications

**Port:** 5005

### Event-Driven Communication Flow

**Example: Customer Places Order**
Customer submits order via Frontend
↓
Frontend → API Gateway → Order Service
↓
Order Service:
Validates cart and stock
Creates order in order_db
Publishes "OrderCreated" event to RabbitMQ
↓
Catalog Service (listening to RabbitMQ):
Receives "OrderCreated" event
Deducts stock from catalog_db
If stock < threshold, publishes "LowStockAlert"
↓
Notification Service (listening to RabbitMQ):
Receives "OrderCreated" event
Pushes update to Kitchen/Cashier via SignalR
↓
Kitchen Dashboard receives WebSocket message
Displays new order instantly

---

## 5. Database Schemas

### Identity Database (identity_db)

**Table: users**
```sql
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  username VARCHAR(100) UNIQUE NOT NULL,
  password_hash VARCHAR(500) NOT NULL,
  role VARCHAR(20) NOT NULL CHECK (role IN ('CASHIER', 'KITCHEN', 'MANAGER', 'ADMIN')),
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Table: audit_logs
sql
CREATE TABLE audit_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE SET NULL,
  action VARCHAR(100) NOT NULL,
  details JSONB,
  ip_address VARCHAR(50),
  user_agent TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Catalog Database (catalog_db)
Table: categories
sql
CREATE TABLE categories (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(100) NOT NULL,
  description TEXT,
  display_order INT DEFAULT 0,
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Table: products
sql
CREATE TABLE products (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  category_id UUID REFERENCES categories(id) ON DELETE CASCADE,
  name VARCHAR(200) NOT NULL,
  description TEXT,
  price DECIMAL(10,2) NOT NULL CHECK (price >= 0),
  image_url VARCHAR(500),
  is_available BOOLEAN DEFAULT true,
  stock_count INT DEFAULT 0 CHECK (stock_count >= 0),
  low_stock_threshold INT DEFAULT 10,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Table: modifiers
sql
CREATE TABLE modifiers (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  product_id UUID REFERENCES products(id) ON DELETE CASCADE,
  group_name VARCHAR(100) NOT NULL,
  is_required BOOLEAN DEFAULT false,
  min_selections INT DEFAULT 0,
  max_selections INT DEFAULT 0,
  display_order INT DEFAULT 0,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Table: modifier_options
sql
CREATE TABLE modifier_options (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  modifier_id UUID REFERENCES modifiers(id) ON DELETE CASCADE,
  name VARCHAR(100) NOT NULL,
  price_add DECIMAL(10,2) DEFAULT 0 CHECK (price_add >= 0),
  display_order INT DEFAULT 0,
  is_available BOOLEAN DEFAULT true,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Order Database (order_db)
Table: tables
sql
CREATE TABLE tables (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  table_number INT UNIQUE NOT NULL CHECK (table_number > 0),
  capacity INT DEFAULT 4 CHECK (capacity > 0),
  qr_code_url VARCHAR(500),
  status VARCHAR(20) DEFAULT 'AVAILABLE' CHECK (status IN ('AVAILABLE', 'OCCUPIED', 'RESERVED', 'CLEANING')),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Table: orders
sql
CREATE TABLE orders (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  table_id UUID REFERENCES tables(id),
  status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'COOKING', 'READY', 'COMPLETED', 'CANCELLED')),
  notes TEXT,
  subtotal DECIMAL(10,2) NOT NULL CHECK (subtotal >= 0),
  tax DECIMAL(10,2) DEFAULT 0 CHECK (tax >= 0),
  discount DECIMAL(10,2) DEFAULT 0 CHECK (discount >= 0),
  total DECIMAL(10,2) NOT NULL CHECK (total >= 0),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  completed_at TIMESTAMP WITH TIME ZONE
);
Table: order_items
sql
CREATE TABLE order_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID REFERENCES orders(id) ON DELETE CASCADE,
  product_id UUID NOT NULL,
  product_name VARCHAR(200) NOT NULL,
  quantity INT NOT NULL CHECK (quantity > 0),
  unit_price DECIMAL(10,2) NOT NULL CHECK (unit_price >= 0),
  selected_modifiers JSONB,
  notes TEXT,
  total_price DECIMAL(10,2) NOT NULL CHECK (total_price >= 0),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
Payment Database (payment_db)
Table: payments
sql
CREATE TABLE payments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID NOT NULL,
  method VARCHAR(20) NOT NULL CHECK (method IN ('CASH', 'QR', 'CARD')),
  amount DECIMAL(10,2) NOT NULL CHECK (amount >= 0),
  received_amount DECIMAL(10,2) CHECK (received_amount >= 0),
  change_amount DECIMAL(10,2) DEFAULT 0 CHECK (change_amount >= 0),
  slip_image_url VARCHAR(500),
  status VARCHAR(30) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'PENDING_VERIFICATION', 'COMPLETED', 'FAILED', 'REFUNDED')),
  verified_by UUID,
  verified_at TIMESTAMP WITH TIME ZONE,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
6. API Documentation
Base URL
http://localhost:5000 (Local Development)
https://api.restaurant.com (Production)
Authentication
All protected endpoints require a JWT token in the Authorization header:
Authorization: Bearer <access_token>
Response Format
All API responses follow this format:
json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "errors": []
}
Key Endpoints
Identity Service:
POST /api/auth/login - Login and receive JWT tokens
POST /api/auth/register - Register a new user (ADMIN only)
POST /api/auth/refresh - Refresh access token
GET /api/auth/users - Get all users (ADMIN only)
Catalog Service:
GET /api/catalog/categories - Get all categories (Public)
GET /api/catalog/products - Get all available products (Public)
GET /api/catalog/products/{id} - Get product by ID (Public)
POST /api/catalog/products - Create a new product (ADMIN/MANAGER)
PATCH /api/catalog/products/{id}/sold-out - Toggle sold-out status (ADMIN/MANAGER)
Order Service:
GET /api/orders/tables - Get all tables with status (Public)
POST /api/orders/cart - Add item to cart (Public)
POST /api/orders/checkout - Create order from cart (Public)
GET /api/orders - Get all orders (staff only)
PATCH /api/orders/{id}/status - Update order status (KITCHEN/MANAGER/ADMIN)
Payment Service:
POST /api/payments/cash - Record cash payment (CASHIER/MANAGER/ADMIN)
POST /api/payments/upload-slip - Upload payment slip photo (Public)
POST /api/payments/{id}/verify - Verify payment (CASHIER/MANAGER/ADMIN)
WebSocket (SignalR)
Connection:
javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5005/hubs/kitchen")
  .withAutomaticReconnect()
  .build();

await connection.start();
Events Received:
OrderCreated - New order placed
OrderStatusChanged - Order status updated
LowStockAlert - Product stock below threshold
7. Frontend Guidelines
Core Design Philosophy
Mobile-First for Customers: 95% of customers will use mid-range smartphones on restaurant Wi-Fi. UI must be thumb-friendly, load instantly, and prevent accidental taps.
High-Contrast & Large Targets for Staff: Kitchen and Cashier screens may be touched with wet/greasy hands or viewed from a distance. Buttons must be large (min 48x48px), and status changes must be visually obvious (color-coded).
Zero Dead Ends: Every action must have a clear success state, error state, or loading state.
Color Palette & Semantics
Primary: Brand color (e.g., Deep Orange/Red for appetite stimulation)
Success: Green (#22c55e) - Order completed, payment verified.
Warning: Yellow/Amber (#f59e0b) - Low stock, order pending verification.
Danger: Red (#ef4444) - Order cancelled, out of stock, payment failed.
Background: Light gray (#f8fafc) for dashboards to reduce eye strain; White for customer menus.
Coding Standards
Use TypeScript strict mode
Use functional components with hooks
Use Zustand for state management
Use React Query for server state
Use Tailwind CSS for styling (no custom CSS files)
Use Shadcn UI components
Implement error boundaries for all routes
Use Next.js App Router conventions
Add JSDoc comments for complex functions
Performance Guidelines
Use React Server Components for static content
Implement code splitting with dynamic imports
Optimize images (Next.js Image component)
Use SWR/React Query for data fetching
Implement loading skeletons for better UX
8. Backend Guidelines
Coding Standards
Use Minimal APIs for all endpoints
Follow C# naming conventions (PascalCase for classes/methods, camelCase for parameters)
Use async/await for all I/O operations
Inject dependencies via constructor injection
Use records for DTOs
Use Entity Framework Core for database access
Use MassTransit for RabbitMQ integration
Implement global exception handling middleware
Use FluentValidation for request validation
Add XML comments for all public APIs
Architecture Rules
NEVER suggest monolithic patterns for backend services
ALWAYS maintain separation between microservices
NEVER share databases between services
ALWAYS use events (RabbitMQ) for inter-service communication
NEVER make synchronous HTTP calls between services for core business logic
Performance Guidelines
Use caching (Redis) for frequently accessed data
Implement pagination for large datasets
Use async/await to prevent blocking
Optimize database queries (avoid N+1 problems)
Use connection pooling for database
9. Security Strategy
Authentication & Authorization
JWT Tokens: Short-lived access tokens (15 min) + long-lived refresh tokens (7 days)
Token Storage: Access token in memory, refresh token in HttpOnly Secure cookie
RBAC: Role-based access control (GUEST, CASHIER, KITCHEN, MANAGER, ADMIN)
Validation: JWT validated at API Gateway AND sensitive microservices
Threat Model & Mitigations
1. Broken Authentication & Session Hijacking
Threat: Attacker steals a staff member's session to void orders or change prices
Mitigation: Access tokens short-lived (15 mins), refresh tokens in HttpOnly cookies, JWT validated on Gateway AND services
2. Insecure Direct Object Reference (IDOR)
Threat: Customer A changes the URL to view or modify Customer B's cart/order
Mitigation: Cart sessions tied to temporary, randomized session_id, staff endpoints enforce RBAC middleware
3. SQL Injection
Threat: Malicious input in product search or order notes alters database queries
Mitigation: Entity Framework Core uses parameterized queries by default. Raw SQL is strictly forbidden.
4. Cross-Site Scripting (XSS)
Threat: Attacker injects malicious scripts via product names or order notes
Mitigation: Next.js automatically escapes output. Content Security Policy (CSP) headers will be enforced.
5. Denial of Service (DoS) / Rate Limiting
Threat: Bot floods the order endpoint, crashing the system or filling the kitchen queue
Mitigation: YARP API Gateway implements rate limiting (e.g., max 5 orders per minute per IP/table)
Security Rules
NEVER trust frontend validation alone
ALWAYS validate JWT and roles on backend
NEVER store secrets in code (use environment variables)
ALWAYS use parameterized queries (prevent SQL injection)
NEVER log sensitive data (passwords, tokens, payment info)
Compliance Checklist
HTTPS enforced everywhere in Production
No secrets committed to Git
Regular dependency updates
Database backups encrypted at rest
Audit logging for all sensitive actions
10. Testing Strategy
The Testing Pyramid
70% Unit Tests: Fast, isolated tests for business logic (e.g., price calculation, modifier validation)
20% Integration Tests: Tests that verify services work with their databases and message brokers
10% End-to-End (E2E) Tests: Critical user journeys (e.g., "Customer scans QR and successfully places an order")
Testing Tools
Backend (.NET): xUnit, Moq (for mocking), FluentAssertions, TestContainers (for real PostgreSQL/RabbitMQ in integration tests)
Frontend (Next.js): Jest / Vitest (unit), Playwright (E2E browser automation)
API Testing: Postman collections or REST Client files committed to the repo
What to Test (Critical Paths)
Cart Calculation: Ensure unit price × quantity + modifier prices = correct total
Stock Deduction: Ensure an OrderCreated event successfully reduces stock_count in the Catalog Service
State Transitions: Ensure an order cannot jump from PENDING directly to COMPLETED (must go through COOKING -> READY)
Auth Guards: Ensure a GUEST token receives a 403 Forbidden when hitting admin endpoints
CI/CD Quality Gates
Pull Requests cannot be merged if:
Unit test coverage drops below 80%
Any E2E test fails
Linting (ESLint / .NET Analyzers) reports errors
11. DevOps & Infrastructure
Environments
Local Development:
Orchestrated via docker-compose.yml
Includes: Frontend, all .NET services, PostgreSQL instances, RabbitMQ
Hot-reload enabled for rapid iteration
Staging:
Exact mirror of Production, but with smaller/cheaper resources
Used for final QA, UAT (User Acceptance Testing), and load testing
Contains anonymized production-like data
Production:
High-availability, auto-scaling, monitored environment
Strict access controls (no direct DB access, all changes via CI/CD)
Cloud Resource Blueprint (AWS Example)
Compute: AWS ECS (Fargate) or EKS for running Docker containers (serverless, no VM management)
Database: Amazon RDS for PostgreSQL (Multi-AZ for high availability, automated daily backups)
Message Broker: Amazon MQ (Managed RabbitMQ) or self-hosted on ECS
API Gateway: Application Load Balancer (ALB) routing to YARP containers
Storage: Amazon S3 (or Cloudflare R2) for product images and payment slip uploads
CDN: Cloudflare in front of the Next.js frontend for caching, DDoS protection, and SSL
CI/CD Pipeline Flow (GitHub Actions)
On Push to main:
Run linters and formatters
Run Unit and Integration tests
Build Docker images
Push images to Container Registry
On Tag Release (e.g., v1.0.0):
Trigger Terraform plan and apply for infrastructure updates
Deploy new container images to Staging
Run E2E Playwright tests against Staging
If passed, deploy to Production (Blue/Green or Rolling deployment)
Observability & Monitoring
Logging: Centralized structured logging (JSON format) sent to a log aggregator (e.g., Grafana Loki or ELK)
Metrics: Prometheus scrapes /metrics endpoints from .NET services (tracking request rate, error rate, latency)
Tracing: OpenTelemetry injects TraceId into HTTP headers and RabbitMQ messages to track a single order across all microservices
Alerting: Slack webhook alerts for: Service downtime, high error rates (>1%), or database CPU > 80%
12. Development Phases
Phase 1: Foundation & Infrastructure (Week 1-2)
Goal: Set up the development environment and core infrastructure
Tasks:
Initialize repository structure
Configure Docker Compose (PostgreSQL, RabbitMQ)
Build API Gateway (YARP) with routing
Implement Identity Service (JWT auth, user management, roles)
Set up CI/CD pipeline (GitHub Actions)
Configure Terraform for cloud infrastructure
Deliverables:
Working API Gateway that routes to services
Login/registration system with JWT
Role-based access control (RBAC)
Docker Compose running all infrastructure
Phase 2: Core Business Services (Week 3-4)
Goal: Build the main business logic services
Tasks:
Build Catalog Service (products, categories, modifiers, stock)
Build Order Service (table sessions, cart, order creation, status transitions)
Implement event-driven communication (RabbitMQ)
Set up SignalR for real-time updates
Create database schemas and migrations
Deliverables:
Complete menu management system
Order placement and tracking
Real-time order updates via WebSockets
Event-driven stock deduction
Phase 3: Frontend - Public Menu (Week 5)
Goal: Build the customer-facing mobile menu
Tasks:
Build Next.js app structure
Create public menu pages (SSR for fast mobile loads)
Implement cart functionality with Zustand
Add modifier selection UI
Integrate with API Gateway
Mobile optimization (touch targets, image loading)
Deliverables:
Fully functional customer menu
Cart system with modifiers
Order submission to backend
Mobile-responsive design
Phase 4: Frontend - Staff Dashboard (Week 6)
Goal: Build the internal staff interface
Tasks:
Create staff login page
Build unified Owner/Kitchen dashboard
Implement real-time order queue (SignalR client)
Add menu management interface (mark sold out, etc.)
Add table management
Implement receipt printing (CSS @media print)
Deliverables:
Staff login system
Live order queue with status updates
Menu and table management
Receipt printing functionality
Phase 5: Payment & Advanced Features (Week 7)
Goal: Complete payment processing and polish
Tasks:
Build Payment Service (cash, QR, slip upload)
Implement payment verification workflow
Add audit logging
Create admin dashboard (reports, staff management)
Add end-of-day (Z-report) functionality
Implement table QR code generation
Deliverables:
Complete payment system
Audit trail for security
Admin reporting dashboard
QR code generation for tables
Phase 6: Testing, DevOps & Deployment (Week 8-9)
Goal: Production readiness
Tasks:
Write unit and integration tests
Load testing and performance optimization
Configure production infrastructure (Terraform)
Deploy to staging environment
Set up monitoring (OpenTelemetry, Prometheus, Grafana)
Security audit and penetration testing
Deploy to production
Deliverables:
Fully tested application
Production infrastructure
Monitoring and alerting
Documentation
Total Timeline: 8-9 weeks (10-11 weeks with buffer)
13. AI Assistant Rules
Your Role
You are a senior full-stack engineer with expertise in:
.NET 10 and C#
Microservices architecture
Next.js and React
PostgreSQL
Event-driven systems
DevOps (Docker, Terraform, CI/CD)
Strict Rules
1. Architecture Compliance
NEVER suggest monolithic patterns for backend services
ALWAYS maintain separation between microservices
NEVER share databases between services
ALWAYS use events (RabbitMQ) for inter-service communication
NEVER make synchronous HTTP calls between services for core business logic
2. Security First
NEVER trust frontend validation alone
ALWAYS validate JWT and roles on backend
NEVER store secrets in code (use environment variables)
ALWAYS use parameterized queries (prevent SQL injection)
NEVER log sensitive data (passwords, tokens, payment info)
3. Code Quality
ALWAYS use TypeScript strict mode in frontend
ALWAYS use async/await in backend
NEVER use any type in TypeScript
ALWAYS add error handling and logging
ALWAYS write unit tests for business logic
NEVER commit code without testing
4. Performance
ALWAYS use pagination for large datasets
NEVER use SELECT * in production
ALWAYS add database indexes for frequently queried columns
ALWAYS use caching for frequently accessed data
NEVER block the main thread (use async operations)
5. Mobile-First
ALWAYS optimize customer menu for mobile devices
ALWAYS use large touch targets (min 44x44px)
ALWAYS optimize images (use Next.js Image component)
NEVER assume fast network (implement loading states)
When I Ask You To...
"Create a new microservice"
Create folder in services/ directory
Set up .NET 10 Minimal API project
Add own PostgreSQL database
Configure RabbitMQ connection
Add Dockerfile
Update docker-compose.yml
Add routing in API Gateway (YARP)
Update architecture documentation
"Add a new API endpoint"
Add endpoint to appropriate service
Add DTO classes (request/response)
Add validation (FluentValidation)
Add authorization attributes
Update API documentation
Add unit tests
Add integration tests
"Add a new database table"
Add entity class in appropriate service
Create migration
Add indexes for performance
Update database documentation
Add seed data (if needed)
Update repository/service layer
"Fix a bug"
Reproduce the bug (write failing test first)
Identify root cause
Fix the issue
Verify test passes
Check for similar issues elsewhere
Update documentation if needed
"Optimize performance"
Profile the code (find bottlenecks)
Add database indexes
Implement caching (Redis)
Optimize queries (avoid N+1)
Add pagination
Use async operations
Measure improvement
14. Common Patterns
Creating a New Endpoint (.NET)
csharp
// Program.cs
app.MapGet("/api/catalog/products/{id}", async (Guid id, ICatalogService service) =>
{
    var product = await service.GetProductByIdAsync(id);
    if (product is null)
        return Results.NotFound();
    
    return Results.Ok(new ApiResponse<ProductDto>
    {
        Success = true,
        Data = product
    });
})
.WithName("GetProductById")
.WithTags("Catalog")
.Produces<ApiResponse<ProductDto>>(200)
.Produces(404)
.RequireAuthorization();
Publishing an Event (RabbitMQ)
csharp
// OrderService - when order is created
await _publishEndpoint.Publish(new OrderCreatedEvent
{
    OrderId = order.Id,
    TableId = order.TableId,
    Items = order.Items,
    Total = order.Total,
    CreatedAt = order.CreatedAt
});
Consuming an Event (RabbitMQ)
csharp
// CatalogService - listens for OrderCreated
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var evt = context.Message;
        
        // Deduct stock for each item
        foreach (var item in evt.Items)
        {
            await _catalogService.DeductStockAsync(item.ProductId, item.Quantity);
        }
    }
}
Real-time Update (SignalR)
csharp
// NotificationService - when order is created
await _hubContext.Clients.Group("kitchen").SendAsync("OrderCreated", new
{
    orderId = order.Id,
    tableNumber = order.TableNumber,
    items = order.Items,
    total = order.Total
});
Frontend API Call (React)
typescript
// Using React Query
const { data, isLoading, error } = useQuery({
  queryKey: ['products'],
  queryFn: async () => {
    const response = await fetch('/api/catalog/products');
    if (!response.ok) throw new Error('Failed to fetch');
    const result = await response.json();
    return result.data;
  }
});
Frontend Real-time (SignalR)
typescript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/kitchen')
  .withAutomaticReconnect()
  .build();

connection.on('OrderCreated', (order) => {
  // Update UI with new order
  setOrders(prev => [order, ...prev]);
});

await connection.start();
15. Troubleshooting
Common Issues
Issue: Services can't connect to RabbitMQ
Solution:
Check RabbitMQ is running: docker ps | grep rabbitmq
Verify connection string in appsettings.json
Check RabbitMQ logs: docker logs pos_rabbitmq
Issue: Database migration fails
Solution:
Check connection string
Verify database exists
Check for conflicting migrations
Rollback and reapply: dotnet ef database update 0 then dotnet ef database update
Issue: JWT validation fails
Solution:
Verify JWT secret is same across all services
Check token expiry
Verify issuer and audience match
Check clock skew (server time sync)
Issue: SignalR connection drops
Solution:
Check WebSocket is enabled in IIS/Kestrel
Verify CORS allows WebSocket
Check network/firewall blocks WebSocket
Implement automatic reconnect on client
Issue: Frontend can't connect to API
Solution:
Verify NEXT_PUBLIC_API_URL in .env.local
Check CORS settings in API Gateway
Ensure API Gateway is running on port 5000
Issue: Services can't connect to PostgreSQL
Solution:
Check docker-compose.yml network configuration
Ensure services are on the same network
Verify connection strings in .env
Deployment Checklist
Before deploying to production:
All tests pass (unit, integration, e2e)
Database migrations tested on staging
Environment variables configured
Secrets loaded from secure vault
HTTPS enabled
CORS configured correctly
Rate limiting enabled
Logging configured (structured logs)
Monitoring set up (OpenTelemetry)
Backup strategy in place
Rollback plan documented
Load testing completed
Security audit passed
Questions to Ask Me
If anything is unclear, ask me:
"Which microservice should handle this feature?"
"Should this be synchronous or event-driven?"
"What's the expected data volume?"
"Are there any security concerns?"
"Should this be cached?"
"What's the SLA for this feature?"
"Do we need to support offline mode?"
Remember
Start simple, iterate fast
Measure before optimizing
Write tests first (TDD when possible)
Document as you go
Ask questions when unsure
Review code before committing
Keep the customer (restaurant owner) in mind
END OF AI CONTEXT FILE
When you start a new AI chat session, tell the AI:
"Read the AI_CONTEXT.md file in the repository root. This contains everything you need to know about the project. Once you understand it, I'll ask you to help with [specific task]."

---

## How to Use This File

1. **Save this file** as `AI_CONTEXT.md` in your repository root
2. **When starting a new AI chat**, tell the AI: "Read the AI_CONTEXT.md file first"
3. **The AI will understand** your entire project, architecture, business logic, tech stack, security requirements, testing strategy, and development phases
4. **You can then ask** the AI to help with any specific task, and it will have full context

This single file contains **everything** an AI needs to help you build this project. No other documentation is required.