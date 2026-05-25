const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PORT = Number(process.env.ACCOUNT_PORT || 8787);
const DB_PATH = path.join(__dirname, "accounts.json");
const sessions = new Map();

function readDb() {
  if (!fs.existsSync(DB_PATH)) return { accounts: {} };
  return JSON.parse(fs.readFileSync(DB_PATH, "utf8"));
}

function writeDb(db) {
  fs.writeFileSync(DB_PATH, JSON.stringify(db, null, 2));
}

function send(res, status, data) {
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Headers": "Content-Type",
    "Access-Control-Allow-Methods": "POST, OPTIONS",
  });
  res.end(JSON.stringify(data));
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.on("data", chunk => {
      body += chunk;
      if (body.length > 1024 * 1024) req.destroy();
    });
    req.on("end", () => {
      try {
        resolve(body ? JSON.parse(body) : {});
      } catch (error) {
        reject(error);
      }
    });
  });
}

function normalize(username) {
  return String(username || "").trim().toLowerCase();
}

function hashPassword(password, salt) {
  return crypto.pbkdf2Sync(String(password || ""), salt, 120000, 32, "sha256").toString("hex");
}

function makeSession(username) {
  const session = crypto.randomBytes(32).toString("hex");
  sessions.set(session, username);
  return session;
}

function getUserBySession(session) {
  return sessions.get(String(session || ""));
}

async function handle(req, res) {
  if (req.method === "OPTIONS") return send(res, 200, { ok: true });
  if (req.method !== "POST") return send(res, 404, { ok: false, message: "Not found" });

  let body;
  try {
    body = await readBody(req);
  } catch {
    return send(res, 400, { ok: false, message: "Bad JSON" });
  }

  const db = readDb();
  const username = normalize(body.username);
  const password = String(body.password || "");

  if (req.url === "/register") {
    if (username.length < 3) return send(res, 400, { ok: false, message: "Логин минимум 3 символа" });
    if (password.length < 6) return send(res, 400, { ok: false, message: "Пароль минимум 6 символов" });
    if (db.accounts[username]) return send(res, 409, { ok: false, message: "Аккаунт уже существует" });

    const salt = crypto.randomBytes(16).toString("hex");
    db.accounts[username] = {
      salt,
      passwordHash: hashPassword(password, salt),
      progress: body.progress || {},
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    writeDb(db);
    return send(res, 200, { ok: true, username, session: makeSession(username), progress: db.accounts[username].progress });
  }

  if (req.url === "/login") {
    const account = db.accounts[username];
    if (!account) return send(res, 404, { ok: false, message: "Аккаунт не найден" });
    if (account.passwordHash !== hashPassword(password, account.salt)) return send(res, 401, { ok: false, message: "Неверный пароль" });
    return send(res, 200, { ok: true, username, session: makeSession(username), progress: account.progress || {} });
  }

  if (req.url === "/progress/load") {
    const sessionUser = getUserBySession(body.session);
    if (!sessionUser) return send(res, 401, { ok: false, message: "Сессия истекла" });
    return send(res, 200, { ok: true, username: sessionUser, progress: db.accounts[sessionUser].progress || {} });
  }

  if (req.url === "/progress/save") {
    const sessionUser = getUserBySession(body.session);
    if (!sessionUser) return send(res, 401, { ok: false, message: "Сессия истекла" });
    db.accounts[sessionUser].progress = body.progress || {};
    db.accounts[sessionUser].updatedAt = new Date().toISOString();
    writeDb(db);
    return send(res, 200, { ok: true });
  }

  send(res, 404, { ok: false, message: "Not found" });
}

http.createServer((req, res) => {
  handle(req, res).catch(error => send(res, 500, { ok: false, message: error.message }));
}).listen(PORT, () => {
  console.log(`Account server listening on port ${PORT}`);
});
