# 📡 API Documentation

## Base URL

```
http://localhost:5000/api
```

## Authentication

All protected endpoints require JWT Bearer token:

```
Authorization: Bearer <token>
```

Get a token by logging in or registering.

---

## Endpoints

### 🔐 Authentication

#### Register

```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:** `201 Created`
```json
{
  "token": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "userId": "507f1f77bcf86cd799439011",
  "email": "user@example.com"
}
```

#### Login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123"
}
```

**Response:** `200 OK`
```json
{
  "token": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "userId": "507f1f77bcf86cd799439011",
  "email": "user@example.com"
}
```

#### Refresh Token

```http
POST /auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "eyJhbGc..."
}
```

**Response:** `200 OK`
```json
{
  "token": "eyJhbGc...",
  "refreshToken": "eyJhbGc..."
}
```

---

### 👥 Users

#### Get Current User Profile

```http
GET /users/profile
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "id": "507f1f77bcf86cd799439011",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "avatar": "https://...",
  "isOnline": true,
  "createdAt": "2026-06-20T10:30:00Z"
}
```

#### Update Profile

```http
PUT /users/profile
Authorization: Bearer <token>
Content-Type: application/json

{
  "firstName": "Johnny",
  "lastName": "Smith",
  "avatar": "https://..."
}
```

**Response:** `200 OK`

#### Change Password

```http
POST /users/change-password
Authorization: Bearer <token>
Content-Type: application/json

{
  "oldPassword": "OldPass123",
  "newPassword": "NewPass456"
}
```

**Response:** `200 OK`

---

### 📇 Contacts

#### Get All Contacts

```http
GET /contacts?page=1&pageSize=20
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "data": [
    {
      "id": "507f1f77bcf86cd799439011",
      "userId": "507f1f77bcf86cd799439012",
      "firstName": "Jane",
      "lastName": "Smith",
      "email": "jane@example.com",
      "avatar": "https://...",
      "isOnline": true,
      "nickname": "Jane S",
      "createdAt": "2026-06-20T10:30:00Z"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 20
}
```

#### Get Contact by ID

```http
GET /contacts/{id}
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "id": "507f1f77bcf86cd799439011",
  "userId": "507f1f77bcf86cd799439012",
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com",
  "avatar": "https://...",
  "isOnline": true,
  "nickname": "Jane S"
}
```

#### Create Contact

```http
POST /contacts
Authorization: Bearer <token>
Content-Type: application/json

{
  "userId": "507f1f77bcf86cd799439012",
  "nickname": "Jane S"
}
```

**Response:** `201 Created`

#### Update Contact

```http
PUT /contacts/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "nickname": "Jane Smith"
}
```

**Response:** `200 OK`

#### Delete Contact

```http
DELETE /contacts/{id}
Authorization: Bearer <token>
```

**Response:** `204 No Content`

#### Block Contact

```http
POST /contacts/{id}/block
Authorization: Bearer <token>
```

**Response:** `200 OK`

#### Unblock Contact

```http
POST /contacts/{id}/unblock
Authorization: Bearer <token>
```

**Response:** `200 OK`

---

### 💬 Messages

#### Get Conversation

```http
GET /messages?contactId={id}&page=1&pageSize=20
Authorization: Bearer <token>
```

**Response:** `200 OK`
```json
{
  "data": [
    {
      "id": "507f1f77bcf86cd799439013",
      "senderId": "507f1f77bcf86cd799439011",
      "senderName": "John Doe",
      "receiverId": "507f1f77bcf86cd799439012",
      "text": "Hello!",
      "attachments": ["https://..."],
      "isRead": true,
      "createdAt": "2026-06-20T10:30:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20
}
```

#### Send Message (REST)

```http
POST /messages
Authorization: Bearer <token>
Content-Type: application/json

{
  "receiverId": "507f1f77bcf86cd799439012",
  "text": "Hello there!",
  "attachments": ["url1", "url2"]
}
```

**Response:** `201 Created`
```json
{
  "id": "507f1f77bcf86cd799439013",
  "senderId": "507f1f77bcf86cd799439011",
  "receiverId": "507f1f77bcf86cd799439012",
  "text": "Hello there!",
  "attachments": ["url1", "url2"],
  "isRead": false,
  "createdAt": "2026-06-20T10:30:00Z"
}
```

#### Mark Message as Read

```http
PUT /messages/{id}/read
Authorization: Bearer <token>
```

**Response:** `200 OK`

#### Delete Message

```http
DELETE /messages/{id}
Authorization: Bearer <token>
```

**Response:** `204 No Content`

---

### 📁 Attachments

#### Upload File

```http
POST /attachments/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

[Binary file data]
```

**Response:** `201 Created`
```json
{
  "id": "507f1f77bcf86cd799439014",
  "fileName": "document.pdf",
  "fileSize": 1024000,
  "fileType": "application/pdf",
  "url": "https://localhost:5000/uploads/507f1f77bcf86cd799439014.pdf",
  "uploadedAt": "2026-06-20T10:30:00Z"
}
```

#### Download File

```http
GET /attachments/{id}/download
Authorization: Bearer <token>
```

**Response:** `200 OK` (file binary)

#### Delete File

```http
DELETE /attachments/{id}
Authorization: Bearer <token>
```

**Response:** `204 No Content`

---

## WebSocket (SignalR)

### Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/chat", {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

connection.start();
```

### Methods (Client → Server)

#### Send Message

```javascript
connection.invoke("SendMessage", {
  receiverId: "507f1f77bcf86cd799439012",
  text: "Hello!",
  attachments: []
})
.catch(err => console.error(err));
```

#### Start Typing

```javascript
connection.invoke("StartTyping", {
  receiverId: "507f1f77bcf86cd799439012"
});
```

#### Stop Typing

```javascript
connection.invoke("StopTyping", {
  receiverId: "507f1f77bcf86cd799439012"
});
```

#### Mark as Read

```javascript
connection.invoke("MarkMessageAsRead", {
  messageId: "507f1f77bcf86cd799439013"
});
```

### Events (Server → Client)

#### Receive Message

```javascript
connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
  // {
  //   id: "507f1f77bcf86cd799439013",
  //   senderId: "507f1f77bcf86cd799439011",
  //   senderName: "John Doe",
  //   text: "Hello!",
  //   attachments: [],
  //   createdAt: "2026-06-20T10:30:00Z"
  // }
});
```

#### User Typing

```javascript
connection.on("UserTyping", (data) => {
  console.log(`${data.userName} is typing...`);
});
```

#### User Stopped Typing

```javascript
connection.on("UserStoppedTyping", (data) => {
  console.log(`${data.userName} stopped typing`);
});
```

#### Online Status Changed

```javascript
connection.on("UserStatusChanged", (data) => {
  console.log(`${data.userName} is ${data.isOnline ? "online" : "offline"}`);
});
```

#### Connection Established

```javascript
connection.on("Connected", (data) => {
  console.log("Connected with connection ID:", data.connectionId);
});
```

---

## Error Responses

### 400 Bad Request
```json
{
  "error": "Invalid request",
  "message": "Email format is invalid",
  "details": [
    {
      "field": "email",
      "message": "Invalid email format"
    }
  ]
}
```

### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "message": "Token is missing or invalid"
}
```

### 403 Forbidden
```json
{
  "error": "Forbidden",
  "message": "You don't have permission to access this resource"
}
```

### 404 Not Found
```json
{
  "error": "Not Found",
  "message": "Resource not found"
}
```

### 409 Conflict
```json
{
  "error": "Conflict",
  "message": "Email already exists"
}
```

### 500 Server Error
```json
{
  "error": "Internal Server Error",
  "message": "An unexpected error occurred",
  "traceId": "0HN1GKQGF3KQU:00000001"
}
```

---

## Rate Limiting

Rate limiting is applied per user:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

When rate limit is exceeded:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
```

---

## Pagination

List endpoints support pagination:

```
GET /contacts?page=1&pageSize=20&sort=createdAt&order=desc
```

**Parameters:**
- `page` - Page number (1-based)
- `pageSize` - Items per page (1-100)
- `sort` - Sort field
- `order` - `asc` or `desc`

**Response:**
```json
{
  "data": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

## Sorting

Supported sort fields:
- `createdAt` - Creation timestamp
- `email` - Email address
- `firstName` - First name
- `lastName` - Last name
- `isOnline` - Online status

---

## Filtering

Some endpoints support filtering:

```
GET /messages?contactId={id}&status=unread&from=2026-06-01&to=2026-06-30
```

---

See also:
- [Architecture](./ARCHITECTURE.md)
- [Setup Guide](./SETUP.md)
- [WebSocket Integration Guide](./WEBSOCKET.md)
