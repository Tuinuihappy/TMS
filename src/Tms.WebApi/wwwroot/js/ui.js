// ═══════════════════════════════════════════════════════════════
// TMS — Reusable UI Components (Modals, Forms, Tables, Toasts)
// ═══════════════════════════════════════════════════════════════

// ── Toast ───────────────────────────────────────────────────
function toast(msg, type = 'info') {
  const c = document.getElementById('toast-container');
  const t = document.createElement('div');
  t.className = `toast ${type}`;
  t.innerHTML = `<span class="toast-msg">${msg}</span>`;
  c.appendChild(t);
  setTimeout(() => { t.style.opacity='0'; setTimeout(()=>t.remove(),300); }, 3500);
}

// ── Helpers ─────────────────────────────────────────────────
const sid = id => id ? id.substring(0,8)+'…' : '—';
const fmtD = d => d ? new Date(d).toLocaleString('th-TH',{dateStyle:'short',timeStyle:'short'}) : '—';
const fmtN = n => n!=null ? Number(n).toLocaleString() : '0';
function badge(status) {
  if (!status) return '';
  const c = status.toLowerCase().replace(/[\s_-]/g,'');
  const m = {intransit:'transit',inprogress:'transit',partialdelivered:'pending',onduty:'transit',available:'confirmed',
             offduty:'draft',inactive:'cancelled',inrepair:'pending',decommissioned:'cancelled',onleave:'draft',
             suspended:'failed',pickedup:'transit',arrived:'dispatched',active:'confirmed',processing:'dispatched'};
  return `<span class="badge badge-${m[c]||c}">${status}</span>`;
}

// ── Modal System ────────────────────────────────────────────
function openModal(title, bodyHtml, opts = {}) {
  closeModal();
  const w = opts.width || '560px';
  const modal = document.createElement('div');
  modal.id = 'modal-overlay';
  modal.style.cssText = `position:fixed;inset:0;z-index:9999;background:rgba(0,0,0,0.6);display:flex;align-items:center;justify-content:center;backdrop-filter:blur(4px);animation:fadeIn .2s ease`;
  modal.innerHTML = `
    <div style="background:var(--bg-secondary);border:1px solid var(--border-default);border-radius:var(--radius-lg);width:${w};max-width:95vw;max-height:90vh;display:flex;flex-direction:column;box-shadow:var(--shadow-lg)">
      <div style="display:flex;justify-content:space-between;align-items:center;padding:18px 24px;border-bottom:1px solid var(--border-default)">
        <h3 style="font-size:16px;font-weight:700">${title}</h3>
        <button onclick="closeModal()" style="background:none;border:none;color:var(--text-muted);font-size:20px;cursor:pointer;padding:4px">✕</button>
      </div>
      <div style="padding:24px;overflow-y:auto;flex:1" id="modal-body">${bodyHtml}</div>
    </div>`;
  modal.addEventListener('click', e => { if (e.target === modal) closeModal(); });
  document.body.appendChild(modal);
}

function closeModal() {
  const m = document.getElementById('modal-overlay');
  if (m) m.remove();
}

// ── Form Builder ────────────────────────────────────────────
function buildForm(fields, opts = {}) {
  const id = opts.formId || 'modal-form';
  let html = `<form id="${id}" style="display:flex;flex-direction:column;gap:14px">`;

  fields.forEach(f => {
    const req = f.required ? ' *' : '';
    const val = f.value !== undefined ? f.value : '';

    if (f.type === 'section') {
      html += `<div style="font-size:12px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:1px;margin-top:8px">${f.label}</div>`;
      return;
    }
    if (f.type === 'hidden') {
      html += `<input type="hidden" name="${f.name}" value="${val}">`;
      return;
    }

    html += `<div>`;
    html += `<label style="font-size:12px;font-weight:500;color:var(--text-secondary);margin-bottom:4px;display:block">${f.label}${req}</label>`;

    if (f.type === 'select') {
      html += `<select name="${f.name}" class="input" ${f.required?'required':''}>`;
      html += `<option value="">— Select —</option>`;
      (f.options||[]).forEach(o => {
        const ov = typeof o === 'object' ? o.value : o;
        const ol = typeof o === 'object' ? o.label : o;
        html += `<option value="${ov}" ${ov==val?'selected':''}>${ol}</option>`;
      });
      html += `</select>`;
    } else if (f.type === 'textarea') {
      html += `<textarea name="${f.name}" class="input" rows="${f.rows||3}" ${f.required?'required':''}>${val}</textarea>`;
    } else if (f.type === 'number') {
      html += `<input type="number" name="${f.name}" class="input" value="${val}" step="${f.step||'any'}" ${f.required?'required':''} ${f.min!=null?`min="${f.min}"`:''}>`;
    } else if (f.type === 'datetime') {
      html += `<input type="datetime-local" name="${f.name}" class="input" value="${val}" ${f.required?'required':''}>`;
    } else if (f.type === 'date') {
      html += `<input type="date" name="${f.name}" class="input" value="${val}" ${f.required?'required':''}>`;
    } else {
      html += `<input type="${f.type||'text'}" name="${f.name}" class="input" value="${val}" ${f.required?'required':''} placeholder="${f.placeholder||''}">`;
    }
    html += `</div>`;
  });

  html += `<div style="display:flex;gap:10px;justify-content:flex-end;margin-top:8px">`;
  html += `<button type="button" onclick="closeModal()" class="btn btn-secondary">Cancel</button>`;
  html += `<button type="submit" class="btn btn-primary">${opts.submitLabel || 'Submit'}</button>`;
  html += `</div></form>`;
  return html;
}

function getFormData(formId) {
  const form = document.getElementById(formId || 'modal-form');
  const fd = new FormData(form);
  const obj = {};
  for (const [k, v] of fd.entries()) {
    // Convert numeric strings
    if (v === '') continue;
    if (!isNaN(v) && v.trim() !== '' && !k.toLowerCase().includes('code') && !k.toLowerCase().includes('number') && !k.toLowerCase().includes('phone') && !k.toLowerCase().includes('postal') && !k.toLowerCase().includes('name') && !k.toLowerCase().includes('email') && !k.toLowerCase().includes('street') && !k.toLowerCase().includes('district') && !k.toLowerCase().includes('province') && !k.toLowerCase().includes('description') && !k.toLowerCase().includes('notes')) {
      obj[k] = Number(v);
    } else {
      obj[k] = v;
    }
  }
  return obj;
}

// ── CRUD Table Builder ──────────────────────────────────────
function buildTable(columns, rows, opts = {}) {
  let html = `<table class="data-table"><thead><tr>`;
  columns.forEach(c => { html += `<th>${c.label}</th>`; });
  if (opts.actions) html += `<th>Actions</th>`;
  html += `</tr></thead><tbody>`;

  if (!rows || rows.length === 0) {
    html += `<tr><td colspan="${columns.length + (opts.actions?1:0)}" style="text-align:center;color:var(--text-muted);padding:30px">No data</td></tr>`;
  } else {
    rows.forEach(r => {
      html += `<tr>`;
      columns.forEach(c => {
        let val = c.render ? c.render(r) : (r[c.key] ?? '—');
        html += `<td>${val}</td>`;
      });
      if (opts.actions) html += `<td style="white-space:nowrap">${opts.actions(r)}</td>`;
      html += `</tr>`;
    });
  }
  html += `</tbody></table>`;
  return html;
}

// ── Action Confirm ──────────────────────────────────────────
async function confirmAction(msg, fn) {
  openModal('⚠️ Confirm Action', `
    <p style="margin-bottom:20px;color:var(--text-secondary)">${msg}</p>
    <div style="display:flex;gap:10px;justify-content:flex-end">
      <button class="btn btn-secondary" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" id="confirm-btn">Confirm</button>
    </div>
  `);
  document.getElementById('confirm-btn').onclick = async () => {
    closeModal();
    try { await fn(); toast('Action completed ✓', 'success'); refreshView(); }
    catch(e) { toast(e.message, 'error'); }
  };
}

// ── JSON Viewer ─────────────────────────────────────────────
function jsonView(data) {
  return `<pre style="background:var(--bg-glass);padding:14px;border-radius:var(--radius-sm);font-size:12px;overflow-x:auto;max-height:400px;color:var(--text-secondary)">${JSON.stringify(data, null, 2)}</pre>`;
}

// ── Detail Modal ────────────────────────────────────────────
function showDetail(title, data) {
  openModal(title, jsonView(data), { width: '700px' });
}
