/* ═══════════════════════════════════════════════════════
   CONFIG
═══════════════════════════════════════════════════════ */
const CONFIG = {
    CURRENT_USER: {
        id: 'usr_me',
        name: 'Cristal Parker',
        initials: 'CP',
        color: '#E1306C',
    },
};

/* ═══════════════════════════════════════════════════════
   SIGNALR
═══════════════════════════════════════════════════════ */
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .withAutomaticReconnect()
    .build();

connection.start()
    .then(() => console.log("SignalR connected"))
    .catch(err => console.error(err));

/* ═══════════════════════════════════════════════════════
   SELECTORS
═══════════════════════════════════════════════════════ */
const sidebar = document.getElementById('sidebar');
const menuBtn = document.getElementById('menu-btn');

const darkToggle = document.getElementById('dark-toggle');

const convList = document.getElementById('conv-list');
const messages = document.getElementById('messages');

const msgField = document.getElementById('msg-field');
const sendBtn = document.getElementById('send-btn');

const scrollFab = document.getElementById('scroll-fab');

const chAv = document.getElementById('ch-av');
const chName = document.getElementById('ch-name');
const chStatus = document.getElementById('ch-status');

let activeConv = null;

/* ═══════════════════════════════════════════════════════
   MENU TOGGLE
═══════════════════════════════════════════════════════ */
menuBtn.addEventListener('click', () => {
    sidebar.classList.toggle('menu-open');
});

/* ═══════════════════════════════════════════════════════
   DARK MODE
═══════════════════════════════════════════════════════ */
darkToggle.addEventListener('click', () => {
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';

    document.documentElement.setAttribute(
        'data-theme',
        isDark ? 'light' : 'dark'
    );

    darkToggle.classList.toggle('on', !isDark);
    darkToggle.setAttribute('aria-checked', String(!isDark));
});

/* ═══════════════════════════════════════════════════════
   RECEIVE MESSAGE
═══════════════════════════════════════════════════════ */
connection.on("ReceiveMessage", (msg) => {
    if (!msg || msg.conversationId !== activeConv) return;

    const time = msg.time || new Date().toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit'
    });

    if (msg.senderId === CONFIG.CURRENT_USER.id) {
        appendOutgoing(msg.content, time, msg.id);
    } else {
        appendIncoming(msg.content, time, msg.sender);
    }

    scrollBottom();
});

/* ═══════════════════════════════════════════════════════
   SWITCH CONVERSATION
═══════════════════════════════════════════════════════ */
function switchConv(id) {
    if (!id) return;

    activeConv = id;

    // highlight selected conversation
    document.querySelectorAll('.conv-item').forEach(el => {
        const isActive = el.dataset.convId === id;
        el.classList.toggle('active', isActive);
    });

    const item = document.querySelector(`[data-conv-id="${id}"]`);
    if (!item) return;

    const name = item.querySelector('.conv-name')?.textContent || "Chat";
    const preview = item.querySelector('.conv-preview')?.textContent || "";

    // update header
    chName.textContent = name;
    chStatus.textContent = preview;
    chAv.textContent = name.substring(0, 2).toUpperCase();

    // 🔥 IMPORTANT: HARD RESET UI BEFORE LOADING
    messages.innerHTML = "";

    // optional: show loading state (so it doesn't look empty bugged)
    const loading = document.createElement("div");
    loading.className = "mg";
    loading.innerHTML = `<div class="ts">Loading messages...</div>`;
    messages.appendChild(loading);

    // join signalR group
    connection.invoke("JoinConversation", id)
        .catch(err => console.log(err));

    // load messages
    loadMessages(id);

    msgField.focus();
}

/* ═══════════════════════════════════════════════════════
   LOAD HISTORY
═══════════════════════════════════════════════════════ */
function loadMessages(conversationId) {
    fetch(`/api/conversations/${conversationId}/messages`)
        .then(r => r.json())
        .then(data => {

            messages.innerHTML = ""; // 🔥 IMPORTANT CLEAN RESET

            if (!data || data.length === 0) {
                messages.innerHTML = `<div class="ts-row"><span class="ts">No messages yet</span></div>`;
                return;
            }

            data.forEach(m => {
                if (m.senderId === CONFIG.CURRENT_USER.id) {
                    appendOutgoing(m.content, m.time, m.id);
                } else {
                    appendIncoming(m.content, m.time, m.sender);
                }
            });

            scrollBottom();
        })
        .catch(() => {
            messages.innerHTML = `<div class="ts-row"><span class="ts">Failed to load messages</span></div>`;
        });
}

/* ═══════════════════════════════════════════════════════
   SEND MESSAGE
═══════════════════════════════════════════════════════ */
function sendMessage() {
    const text = msgField.value.trim();
    if (!text || !activeConv) return;

    const time = new Date().toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit'
    });

    appendOutgoing(text, time, "pending");

    msgField.value = "";
    scrollBottom();

    connection.invoke("SendMessage", {
        conversationId: activeConv,
        content: text,
        senderId: CONFIG.CURRENT_USER.id,
        senderName: CONFIG.CURRENT_USER.name,
        senderInitials: CONFIG.CURRENT_USER.initials,
        time: time
    });
}

/* ═══════════════════════════════════════════════════════
   RENDER MESSAGES
═══════════════════════════════════════════════════════ */
function appendOutgoing(text, time, id) {
    const el = document.createElement("div");

    el.innerHTML = `
        <div class="mg"></div>
        <div class="msg-row out" data-msg-id="${id}">
            <div class="bubble">${escapeHtml(text)}</div>
        </div>
        <div class="ts-row out">
            <span class="ts">${time} <span class="tick">✓</span></span>
        </div>
    `;

    messages.appendChild(el);
}

function appendIncoming(text, time, meta) {
    const el = document.createElement("div");

    el.innerHTML = `
        <div class="mg"></div>
        <div class="msg-row in">
            <div class="msg-av-wrap">
                <div class="av av-sm" style="background:${meta?.color || '#999'};">
                    ${meta?.initials || "??"}
                </div>
            </div>
            <div class="bubble">${escapeHtml(text)}</div>
        </div>
        <div class="ts-row">
            <span class="ts">${time}</span>
        </div>
    `;

    messages.appendChild(el);
}

/* ═══════════════════════════════════════════════════════
   HELPERS
═══════════════════════════════════════════════════════ */
function scrollBottom() {
    messages.scrollTop = messages.scrollHeight;
}

function escapeHtml(s) {
    return s.replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");
}

/* ═══════════════════════════════════════════════════════
   EVENTS
═══════════════════════════════════════════════════════ */
convList.addEventListener('click', (e) => {
    const item = e.target.closest('.conv-item');
    if (item?.dataset.convId) switchConv(item.dataset.convId);
});

sendBtn.addEventListener('click', sendMessage);

msgField.addEventListener('keydown', (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

/* ═══════════════════════════════════════════════════════
   INIT
═══════════════════════════════════════════════════════ */
document.addEventListener("DOMContentLoaded", () => {
    messages.innerHTML = `
        <div class="ts-row">
            <span class="ts">Select a conversation</span>
        </div>
    `;

    activeConv = null;
});