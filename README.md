## Cuprins
1. Introducere ................................................ 3
2. Arhitectura Proiectului .................................... 3
3. Structura Bazei de Date .................................... 4
4. Funcționalități Principale ................................. 5
5. Git, CI/CD și Deploy ...................................... 6
6. Concluzie ................................................. 7


## 1. Introducere

NotePad+ este o aplicație web enterprise dezvoltată folosind React și .NET, având scopul de a oferi utilizatorilor un mediu avansat pentru crearea, organizarea și partajarea notițelor. Aplicația pune accent pe colaborare în timp real și securitate avansată.

## 2. Arhitectura Proiectului

Proiectul este organizat în două componente principale:
- Frontend (React)
- Backend (.NET Core)

### Frontend
- src/
  - components/: Componente React reutilizabile
  - pages/: Paginile aplicației
  - services/: Servicii pentru API și autentificare
  - utils/: Utilități și helper functions

### Backend
- Controllers/: API endpoints
- Services/: Logica de business
- Models/: Entitățile bazei de date
- Data/: Context și configurare bază de date

## 3. Structura Bazei de Date

### Entități Principale

#### User
- Id: int (PK)
- Username: string
- PasswordHash: string
- Email: string
- TwoFactorEnabled: bool
- CreatedAt: DateTime

#### Note
- Id: int (PK)
- Title: string
- Content: text
- UserId: int (FK)
- FolderId: int (FK)
- CreatedAt: DateTime
- UpdatedAt: DateTime

#### Folder
- Id: int (PK)
- Name: string
- ParentId: int (FK)
- UserId: int (FK)
- CreatedAt: DateTime

## 4. Funcționalități Principale

### Autentificare & Securitate
- /api/auth/login: Autentificare cu JWT
- /api/auth/register: Înregistrare utilizator nou
- /api/auth/2fa: Configurare autentificare doi factori
- /api/auth/refresh: Reînnoire token JWT

### Managementul Notițelor
- /api/notes: CRUD pentru notițe
- /api/folders: Organizare ierarhică
- /api/share: Partajare și permisiuni
- /api/search: Căutare full-text
