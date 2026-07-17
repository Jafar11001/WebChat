/* ═══════════════════════════════════════════════════════
   CONFIG
   The signed-in user comes from the server (window.CURRENT_USER,
   set in Index.cshtml from Identity). Never hardcode it here —
   this file used to fake a user, which made every message look
   like it was sent by the same person.
═══════════════════════════════════════════════════════ */
const CURRENT_USER = window.CURRENT_USER;

if (!CURRENT_USER) {
    // Without an identity we can't tell incoming from outgoing, so fail
    // loudly instead of silently mislabelling every message.
    throw new Error('window.CURRENT_USER missing — is the page being served by HomeController.Index?');
}

/* ═══════════════════════════════════════════════════════
   DATA
   The database is the single source of truth for conversations
   and messages. CONVERSATIONS is just an in-memory cache of the
   last GET /api/conversations, so the sidebar can re-render
   (highlight, preview) without a round trip every time.
═══════════════════════════════════════════════════════ */
let CONVERSATIONS = [];
let activeConv = null;
let searchQuery = '';

/* ═══════════════════════════════════════════════════════
   SELECTORS
═══════════════════════════════════════════════════════ */
const sidebar = document.getElementById('sidebar');
const appMenu = document.getElementById('app-menu');
const menuBtn = document.getElementById('menu-btn');
const darkItem = document.getElementById('dark-item');
const darkToggle = document.getElementById('dark-toggle');
const searchInput = document.getElementById('search-input');

const convList = document.getElementById('conv-list');
const messages = document.getElementById('messages');

const msgField = document.getElementById('msg-field');
const sendBtn = document.getElementById('send-btn');

const scrollFab = document.getElementById('scroll-fab');

const chatHeader = document.getElementById('chat-header');
const chAv = document.getElementById('ch-av');
const chOnlineDot = document.getElementById('ch-online-dot');
const chName = document.getElementById('ch-name');
const chDot = document.getElementById('ch-dot');
const chStatus = document.getElementById('ch-status');

const currentUserAv = document.getElementById('current-user-av');
const currentUserName = document.getElementById('current-user-name');
const currentUserEmail = document.getElementById('current-user-email');

const userPicker = document.getElementById('user-picker');
const userPickerClose = document.getElementById('user-picker-close');
const userPickerSearch = document.getElementById('user-picker-search');
const userList = document.getElementById('user-list');

/* ═══════════════════════════════════════════════════════
   HELPERS
═══════════════════════════════════════════════════════ */
function escapeHtml(s) {
    return String(s ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

// Colours land inside style="..." attributes. Anything that isn't a plain
// CSS colour gets dropped rather than escaped — no reason to be clever.
function safeColor(c) {
    return /^#[0-9a-f]{3,8}$/i.test(String(c ?? '')) ? c : '#999999';
}

function nowTime() {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function newClientId() {
    return (crypto.randomUUID?.() ?? `c${Date.now()}-${Math.random().toString(16).slice(2)}`);
}

function scrollBottom() {
    messages.scrollTop = messages.scrollHeight;
}

function isNearBottom() {
    return messages.scrollHeight - messages.scrollTop - messages.clientHeight < 120;
}

/* ═══════════════════════════════════════════════════════
   SIGNALR
   The hub addresses us by user id, not by group, so there's
   nothing to join and nothing to rejoin after a reconnect — we
   receive every message for every conversation we're in.

   withAutomaticReconnect only covers drops AFTER a successful
   start, so the initial connect gets its own retry. sendMessage
   awaits `ready` first — otherwise a click that lands before the
   handshake throws "Cannot send data if the connection is not in
   the 'Connected' State".
═══════════════════════════════════════════════════════ */
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/chathub')
    .withAutomaticReconnect()
    .build();

let ready = start();

async function start(attempt = 0) {
    try {
        await connection.start();
        console.log('SignalR connected');
    } catch (err) {
        const wait = Math.min(1000 * 2 ** attempt, 30000);
        console.error(`SignalR connect failed, retrying in ${wait}ms`, err);
        await new Promise(r => setTimeout(r, wait));
        return start(attempt + 1);
    }
}

connection.onreconnected(() => {
    // Messages sent while we were offline aren't replayed, so resync.
    console.log('SignalR reconnected');
    loadConversations();
    if (activeConv) loadMessages(activeConv);
});

connection.onclose(err => {
    console.error('SignalR closed, restarting', err);
    ready = start();
});

/* ═══════════════════════════════════════════════════════
   LOAD + RENDER: SIDEBAR CONVERSATION LIST
═══════════════════════════════════════════════════════ */
async function loadConversations() {
    try {
        const res = await fetch('/api/conversations');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        CONVERSATIONS = await res.json();
    } catch (err) {
        console.error('Failed to load conversations:', err);
        CONVERSATIONS = [];
        convList.innerHTML = `<li class="list-section-label">Couldn't load conversations</li>`;
        return;
    }

    renderConversationList();
}

function renderConversationList() {
    const q = searchQuery.trim().toLowerCase();
    const visible = q
        ? CONVERSATIONS.filter(c => (c.title || '').toLowerCase().includes(q))
        : CONVERSATIONS;

    convList.innerHTML = '';

    if (visible.length === 0) {
        convList.innerHTML = `<li class="list-section-label">${q ? 'No matches' : 'No conversations yet'}</li>`;
        return;
    }

    visible.forEach(conv => {
        const li = document.createElement('li');
        li.className = 'conv-item';
        li.dataset.convId = conv.id;
        li.setAttribute('role', 'option');
        li.setAttribute('aria-selected', String(conv.id === activeConv));
        li.classList.toggle('active', conv.id === activeConv);
        li.tabIndex = 0;

        li.innerHTML = `
            <div class="av-wrap">
                <div class="av av-lg" style="background:${safeColor(conv.avatarColor)};">${escapeHtml(conv.avatarInitials)}</div>
                ${conv.isOnline ? '<span class="online-dot"></span>' : ''}
            </div>
            <div class="conv-body">
                <div class="conv-name">${escapeHtml(conv.title)}</div>
                <div class="conv-preview">${escapeHtml(conv.lastMessage || 'No messages yet')}</div>
            </div>
            <div class="conv-meta">
                <span class="conv-time">${escapeHtml(conv.lastMessageTime || '')}</span>
            </div>
        `;

        convList.appendChild(li);
    });
}

/* ═══════════════════════════════════════════════════════
   HEADER: empty vs. active states
═══════════════════════════════════════════════════════ */
function resetHeader() {
    chatHeader.classList.add('is-empty');
    delete chatHeader.dataset.convId;

    chAv.style.background = '';
    chAv.textContent = '';
    chOnlineDot.style.display = 'none';

    chName.textContent = 'Select a conversation';
    chStatus.textContent = '';
    chDot.classList.remove('off');
}

function updateHeader(conv) {
    chatHeader.classList.remove('is-empty');
    chatHeader.dataset.convId = conv.id;

    chAv.style.background = safeColor(conv.avatarColor);
    chAv.textContent = conv.avatarInitials;
    chOnlineDot.style.display = conv.isOnline ? '' : 'none';

    chName.textContent = conv.title;
    chStatus.textContent = conv.isOnline ? 'Active now' : 'Offline';
    chDot.classList.toggle('off', !conv.isOnline);
}

/* ═══════════════════════════════════════════════════════
   MESSAGES
═══════════════════════════════════════════════════════ */
function renderEmptyState(title, sub) {
    messages.innerHTML = '';
    const el = document.createElement('div');
    el.className = 'empty-state';
    el.innerHTML = `
        <svg width="46" height="46" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z" />
        </svg>
        <div class="empty-state-title">${escapeHtml(title)}</div>
        <div class="empty-state-sub">${escapeHtml(sub)}</div>
    `;
    messages.appendChild(el);
    updateScrollFab();
}

async function loadMessages(conversationId) {
    renderEmptyState('Loading messages…', '');

    try {
        const res = await fetch(`/api/conversations/${encodeURIComponent(conversationId)}/messages`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const history = await res.json();

        // The user may have switched conversations while this was in
        // flight — don't paint stale results over the new one.
        if (conversationId !== activeConv) return;

        messages.innerHTML = '';

        if (!history || history.length === 0) {
            renderEmptyState('No messages yet', 'Say hello 👋');
            return;
        }

        history.forEach(m => {
            if (m.senderId === CURRENT_USER.id) {
                appendOutgoing(m.content, m.time, { id: m.id });
            } else {
                appendIncoming(m.content, m.time, { initials: m.senderInitials, color: m.senderColor });
            }
        });

        scrollBottom();
    } catch (err) {
        console.error('Failed to load messages:', err);
        if (conversationId !== activeConv) return;
        renderEmptyState('Failed to load messages', 'Please try again');
    }
}

/* ═══════════════════════════════════════════════════════
   RECEIVE MESSAGE (SignalR)
   The hub broadcasts to the whole group — including the sender.
   For our own messages the optimistic bubble is already on
   screen, so we reconcile it by clientId instead of appending a
   duplicate.
═══════════════════════════════════════════════════════ */
connection.on('ReceiveMessage', (msg) => {
    if (!msg || !msg.conversationId) return;

    const time = msg.time || nowTime();

    // First message of a DM someone just opened with us — we've never seen
    // this conversation, so pull the sidebar again to make it appear.
    if (!CONVERSATIONS.some(c => c.id === msg.conversationId)) {
        loadConversations();
        return;
    }

    updateConvPreview(msg.conversationId, msg.content, time);

    if (msg.conversationId !== activeConv) return;

    const mine = msg.senderId === CURRENT_USER.id;

    if (mine && msg.clientId) {
        const pending = messages.querySelector(`[data-client-id="${CSS.escape(msg.clientId)}"]`);
        if (pending) {
            // Our own echo: confirm the bubble we already drew.
            pending.dataset.msgId = msg.id;
            delete pending.dataset.clientId;
            pending.classList.remove('pending', 'failed');
            return;
        }
    }

    const stick = isNearBottom();

    if (mine) {
        appendOutgoing(msg.content, time, { id: msg.id });
    } else {
        appendIncoming(msg.content, time, msg.sender);
    }

    // Don't yank someone away from history they're reading.
    if (stick) scrollBottom();
    updateScrollFab();
});

/* ═══════════════════════════════════════════════════════
   SWITCH CONVERSATION
═══════════════════════════════════════════════════════ */
function switchConv(id) {
    const conv = CONVERSATIONS.find(c => c.id === id);
    if (!conv || id === activeConv) return;

    activeConv = id;

    document.querySelectorAll('.conv-item').forEach(el => {
        const isActive = el.dataset.convId === id;
        el.classList.toggle('active', isActive);
        el.setAttribute('aria-selected', String(isActive));
    });

    updateHeader(conv);
    loadMessages(id);
    msgField.focus();
}

/* ═══════════════════════════════════════════════════════
   SEND MESSAGE
   The hub persists it and broadcasts it back to everyone in the
   group (us included) via ReceiveMessage.
═══════════════════════════════════════════════════════ */
async function sendMessage() {
    const text = msgField.value.trim();
    if (!text || !activeConv) return;

    const conversationId = activeConv;
    const clientId = newClientId();
    const time = nowTime();

    // Optimistic bubble — reconciled when the server echoes clientId back.
    appendOutgoing(text, time, { clientId, pending: true });
    updateConvPreview(conversationId, text, time);

    msgField.value = '';
    scrollBottom();

    try {
        await ready;
        await connection.invoke('SendMessage', { conversationId, content: text, clientId });
    } catch (err) {
        console.error('Failed to send message:', err);
        const failed = messages.querySelector(`[data-client-id="${CSS.escape(clientId)}"]`);
        failed?.classList.add('failed');
        const tick = failed?.querySelector('.tick');
        if (tick) tick.textContent = '!';
    }
}

/* ═══════════════════════════════════════════════════════
   SIDEBAR PREVIEW SYNC
═══════════════════════════════════════════════════════ */
function updateConvPreview(conversationId, text, time) {
    const conv = CONVERSATIONS.find(c => c.id === conversationId);
    if (conv) {
        conv.lastMessage = text;
        conv.lastMessageTime = time;
    }

    const item = convList.querySelector(`.conv-item[data-conv-id="${CSS.escape(conversationId)}"]`);
    if (!item) return;

    const preview = item.querySelector('.conv-preview');
    const timeEl = item.querySelector('.conv-time');
    if (preview) preview.textContent = text;
    if (timeEl) timeEl.textContent = time;
}

/* ═══════════════════════════════════════════════════════
   RENDER MESSAGE ROWS
═══════════════════════════════════════════════════════ */
function appendOutgoing(text, time, { id, clientId, pending } = {}) {
    const el = document.createElement('div');
    el.className = 'msg-group' + (pending ? ' pending' : '');
    if (id != null) el.dataset.msgId = id;
    if (clientId) el.dataset.clientId = clientId;

    el.innerHTML = `
        <div class="mg"></div>
        <div class="msg-row out">
            <div class="bubble">${escapeHtml(text)}</div>
        </div>
        <div class="ts-row out">
            <span class="ts">${escapeHtml(time)} <span class="tick">✓</span></span>
        </div>
    `;

    messages.appendChild(el);
}

function appendIncoming(text, time, meta) {
    const el = document.createElement('div');
    el.className = 'msg-group';

    el.innerHTML = `
        <div class="mg"></div>
        <div class="msg-row in">
            <div class="msg-av-wrap">
                <div class="av av-sm" style="background:${safeColor(meta?.color)};">
                    ${escapeHtml(meta?.initials || '??')}
                </div>
            </div>
            <div class="bubble">${escapeHtml(text)}</div>
        </div>
        <div class="ts-row">
            <span class="ts">${escapeHtml(time)}</span>
        </div>
    `;

    messages.appendChild(el);
}

/* ═══════════════════════════════════════════════════════
   APP MENU
═══════════════════════════════════════════════════════ */
function setMenuOpen(open) {
    sidebar.classList.toggle('menu-open', open);
    menuBtn.setAttribute('aria-expanded', String(open));
}

menuBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    setMenuOpen(!sidebar.classList.contains('menu-open'));
});

document.addEventListener('click', (e) => {
    if (!sidebar.classList.contains('menu-open')) return;
    if (appMenu.contains(e.target) || menuBtn.contains(e.target)) return;
    setMenuOpen(false);
});

document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape') return;
    setMenuOpen(false);
    if (!userPicker.hidden) closeUserPicker();
});

/* ═══════════════════════════════════════════════════════
   DARK MODE
   The whole row is the hit target, not just the little switch.
═══════════════════════════════════════════════════════ */
function applyTheme(dark) {
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    darkToggle.classList.toggle('on', dark);
    darkToggle.setAttribute('aria-checked', String(dark));
    darkItem.setAttribute('aria-pressed', String(dark));
    localStorage.setItem('theme', dark ? 'dark' : 'light');
}

darkItem.addEventListener('click', () => {
    applyTheme(document.documentElement.getAttribute('data-theme') !== 'dark');
});

/* ═══════════════════════════════════════════════════════
   NEW DIRECT MESSAGE
   Picks a real user and opens (or reuses) the DM with them.
═══════════════════════════════════════════════════════ */
let USERS = [];

async function openUserPicker() {
    setMenuOpen(false);
    userPicker.hidden = false;
    userPickerSearch.value = '';
    userList.innerHTML = `<li class="user-list-empty">Loading…</li>`;
    userPickerSearch.focus();

    try {
        const res = await fetch('/api/users');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        USERS = await res.json();
    } catch (err) {
        console.error('Failed to load users:', err);
        userList.innerHTML = `<li class="user-list-empty">Couldn't load people</li>`;
        return;
    }

    renderUserList();
}

function closeUserPicker() {
    userPicker.hidden = true;
}

function renderUserList() {
    const q = userPickerSearch.value.trim().toLowerCase();
    const visible = q ? USERS.filter(u => u.name.toLowerCase().includes(q)) : USERS;

    userList.innerHTML = '';

    if (visible.length === 0) {
        userList.innerHTML = `<li class="user-list-empty">${
            USERS.length === 0 ? 'Nobody else has registered yet' : 'No matches'
        }</li>`;
        return;
    }

    visible.forEach(u => {
        const li = document.createElement('li');
        li.className = 'user-item';
        li.dataset.userId = u.id;
        li.setAttribute('role', 'option');
        li.tabIndex = 0;
        li.innerHTML = `
            <div class="av av-md" style="background:${safeColor(u.color)};">${escapeHtml(u.initials)}</div>
            <div class="user-item-name">${escapeHtml(u.name)}</div>
        `;
        userList.appendChild(li);
    });
}

async function startDirect(userId) {
    try {
        const res = await fetch('/api/conversations/direct', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId })
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const conv = await res.json();

        closeUserPicker();

        // The DM may be brand new or one we already had — reload either way so
        // the sidebar matches the server, then open it.
        await loadConversations();
        switchConv(conv.id);
    } catch (err) {
        console.error('Failed to open direct message:', err);
        userList.innerHTML = `<li class="user-list-empty">Couldn't open that chat</li>`;
    }
}

userPickerSearch.addEventListener('input', renderUserList);
userPickerClose.addEventListener('click', closeUserPicker);

userPicker.addEventListener('click', (e) => {
    if (e.target === userPicker) closeUserPicker();
});

userList.addEventListener('click', (e) => {
    const item = e.target.closest('.user-item');
    if (item?.dataset.userId) startDirect(item.dataset.userId);
});

userList.addEventListener('keydown', (e) => {
    if (e.key !== 'Enter' && e.key !== ' ') return;
    const item = e.target.closest('.user-item');
    if (item?.dataset.userId) {
        e.preventDefault();
        startDirect(item.dataset.userId);
    }
});

/* ═══════════════════════════════════════════════════════
   MENU ACTIONS
═══════════════════════════════════════════════════════ */
appMenu.addEventListener('click', (e) => {
    const item = e.target.closest('.menu-item');
    const action = item?.dataset.action;
    if (!action || action === 'dark-mode') return;

    if (action === 'sign-out') {
        // POST via the hidden form so the antiforgery token is sent.
        document.getElementById('logout-form')?.submit();
        return;
    }

    if (action === 'new-dm') {
        openUserPicker();
        return;
    }

    // mentions / new-group have no backend yet.
    console.warn(`Menu action "${action}" is not implemented yet.`);
});

/* ═══════════════════════════════════════════════════════
   SCROLL FAB
   CSS hides it by default and reveals it on .show — nothing ever
   added that class, so the button was permanently invisible.
═══════════════════════════════════════════════════════ */
function updateScrollFab() {
    scrollFab.classList.toggle('show', !isNearBottom());
}

messages.addEventListener('scroll', updateScrollFab);
scrollFab.addEventListener('click', scrollBottom);

/* ═══════════════════════════════════════════════════════
   EVENTS
═══════════════════════════════════════════════════════ */
convList.addEventListener('click', (e) => {
    const item = e.target.closest('.conv-item');
    if (item?.dataset.convId) switchConv(item.dataset.convId);
});

convList.addEventListener('keydown', (e) => {
    if (e.key !== 'Enter' && e.key !== ' ') return;
    const item = e.target.closest('.conv-item');
    if (item?.dataset.convId) {
        e.preventDefault();
        switchConv(item.dataset.convId);
    }
});

searchInput.addEventListener('input', () => {
    searchQuery = searchInput.value;
    renderConversationList();
});

sendBtn.addEventListener('click', sendMessage);

msgField.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

/* ═══════════════════════════════════════════════════════
   INIT
   Page load = no conversation selected. The list comes from the
   API; header and messages stay empty until the user picks one.
═══════════════════════════════════════════════════════ */
applyTheme(localStorage.getItem('theme') === 'dark');

currentUserAv.textContent = CURRENT_USER.initials;
currentUserAv.style.background = safeColor(CURRENT_USER.color);
currentUserName.textContent = CURRENT_USER.name;
currentUserEmail.textContent = CURRENT_USER.email;

resetHeader();
renderEmptyState('Select a conversation', 'Choose someone from the list to start chatting');
loadConversations();
