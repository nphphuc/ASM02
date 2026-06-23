// ============================================================
// AI Chatbot Sinh Viên — Global JavaScript
// ============================================================

const App = {
    // ── SignalR Chat ─────────────────────────────
    connection: null,
    currentSessionId: null,

    async initChat(sessionId) {
        this.currentSessionId = sessionId;
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.on('ReceiveMessage', (msg) => {
            this.appendMessage(msg.role, msg.content, msg.sources, msg.responseTimeMs);
        });

        this.connection.on('Typing', (isTyping) => {
            const indicator = document.getElementById('typingIndicator');
            if (indicator) indicator.style.display = isTyping ? 'flex' : 'none';
            this.scrollToBottom();
        });

        this.connection.on('Error', (error) => {
            this.appendMessage('Assistant', `❌ Lỗi: ${error}`, null, null);
            this.hideTyping();
        });

        try {
            await this.connection.start();
            await this.connection.invoke('JoinSession', sessionId);
            console.log('SignalR connected, session:', sessionId);
        } catch (err) {
            console.error('SignalR error:', err);
        }
    },

    async sendMessage() {
        const textarea = document.getElementById('chatInput');
        const question = textarea.value.trim();
        if (!question || !this.currentSessionId) return;

        textarea.value = '';
        textarea.style.height = 'auto';
        this.appendMessage('User', question, null, null);
        this.showTyping();

        try {
            await this.connection.invoke('SendMessage', this.currentSessionId, question);
        } catch (err) {
            console.error('Send error:', err);
            this.appendMessage('Assistant', '❌ Không thể gửi tin nhắn. Vui lòng thử lại.', null, null);
            this.hideTyping();
        }
    },

    appendMessage(role, content, sources, responseTimeMs) {
        const container = document.getElementById('chatMessages');
        if (!container) return;

        const isUser = role === 'User';
        const avatar = isUser
            ? '<i class="bi bi-person-fill"></i>'
            : '<i class="bi bi-robot"></i>';

        const formattedContent = this.formatMessage(content);
        let sourcesHtml = '';
        if (sources && sources.length > 0) {
            sourcesHtml = '<div class="sources-panel mt-2"><strong><i class="bi bi-link-45deg"></i> Nguồn tham khảo:</strong>';
            sources.forEach(s => {
                sourcesHtml += `<div class="source-item">
                    <span class="source-score">${(s.score * 100).toFixed(0)}%</span>
                    <span>${s.documentName} (chunk #${s.chunkPosition})</span>
                </div>`;
            });
            sourcesHtml += '</div>';
        }

        const timeMeta = responseTimeMs
            ? `<div class="message-meta"><i class="bi bi-clock"></i> ${responseTimeMs}ms</div>`
            : '';

        const html = `<div class="message ${role.toLowerCase()}">
            <div class="message-avatar">${avatar}</div>
            <div>
                <div class="message-bubble">${formattedContent}</div>
                ${sourcesHtml}
                ${timeMeta}
            </div>
        </div>`;

        container.insertAdjacentHTML('beforeend', html);
        this.scrollToBottom();
    },

    formatMessage(text) {
        if (!text) return '';
        return text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/\n/g, '<br>')
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/`(.*?)`/g, '<code>$1</code>');
    },

    showTyping() {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) indicator.style.display = 'flex';
        this.scrollToBottom();
    },

    hideTyping() {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) indicator.style.display = 'none';
    },

    scrollToBottom() {
        const container = document.getElementById('chatMessages');
        if (container) {
            setTimeout(() => container.scrollTop = container.scrollHeight, 50);
        }
    },

    // ── Upload Zone ───────────────────────────────
    initUploadZone() {
        const zone = document.getElementById('uploadZone');
        const fileInput = document.getElementById('fileInput');
        if (!zone || !fileInput) return;

        zone.addEventListener('click', () => fileInput.click());
        zone.addEventListener('dragover', (e) => { e.preventDefault(); zone.classList.add('dragover'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('dragover'));
        zone.addEventListener('drop', (e) => {
            e.preventDefault();
            zone.classList.remove('dragover');
            fileInput.files = e.dataTransfer.files;
            this.previewFiles(fileInput.files);
        });

        fileInput.addEventListener('change', () => this.previewFiles(fileInput.files));
    },

    previewFiles(files) {
        const preview = document.getElementById('filePreview');
        if (!preview || !files.length) return;

        let html = '<div class="mt-3">';
        for (const f of files) {
            const icon = this.getFileIcon(f.name);
            const size = (f.size / 1024 / 1024).toFixed(2);
            html += `<div class="d-flex align-items-center gap-2 p-2 bg-light rounded mb-1">
                <i class="${icon} text-primary"></i>
                <span class="flex-grow-1">${f.name}</span>
                <span class="text-muted small">${size} MB</span>
            </div>`;
        }
        html += '</div>';
        preview.innerHTML = html;
    },

    getFileIcon(filename) {
        const ext = filename.split('.').pop().toLowerCase();
        switch (ext) {
            case 'pdf': return 'bi bi-file-earmark-pdf';
            case 'docx': case 'doc': return 'bi bi-file-earmark-word';
            case 'pptx': case 'ppt': return 'bi bi-file-earmark-slides';
            default: return 'bi bi-file-earmark';
        }
    },

    // ── Chat Input Auto-resize ────────────────────
    initAutoResize() {
        const textarea = document.getElementById('chatInput');
        if (!textarea) return;
        textarea.addEventListener('input', function () {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 120) + 'px';
        });
        textarea.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    },

    // ── Benchmark Charts ──────────────────────────
    renderComparisonChart(canvasId, data) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        new Chart(ctx, {
            type: 'radar',
            data: {
                labels: ['Faithfulness', 'Answer Relevance', 'Context Precision', 'Context Recall'],
                datasets: data.map((d, i) => ({
                    label: d.name,
                    data: [d.faithfulness, d.answerRelevance, d.contextPrecision, d.contextRecall],
                    backgroundColor: `rgba(${79 + i * 50}, ${70 + i * 30}, ${229 - i * 40}, .15)`,
                    borderColor: `rgba(${79 + i * 50}, ${70 + i * 30}, ${229 - i * 40}, 1)`,
                    borderWidth: 2,
                    pointRadius: 4,
                }))
            },
            options: {
                responsive: true,
                scales: { r: { beginAtZero: true, max: 1, ticks: { stepSize: 0.2 } } },
                plugins: { legend: { position: 'bottom' } }
            }
        });
    },

    renderBarChart(canvasId, labels, datasets) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        new Chart(ctx, {
            type: 'bar',
            data: { labels, datasets },
            options: {
                responsive: true,
                plugins: { legend: { position: 'top' } },
                scales: {
                    y: { beginAtZero: true, max: 1, title: { display: true, text: 'Score' } }
                }
            }
        });
    }
};
