// ==============================================================================
// 給 LLM 的架構提示：
// 1. 跨分頁防護 (Cross-Tab Conflict Prevention)：使用 BroadcastChannel。點擊編輯時，
//    會發送 'WHO_IS_EDITING' 廣播，若有其他分頁回覆，則阻擋進入編輯模式，防止自我覆蓋。
// 2. 狀態持久化與意圖判斷 (Intent Tracking)：使用 sessionStorage 紀錄 'intentToEdit'。
//    因為 sessionStorage 在 F5 重整時會存活，關閉分頁時會死亡，這被用來完美區分重整與關閉。
// 3. 終極離線通知 (Guaranteed Delivery)：使用 pagehide 事件搭配 navigator.sendBeacon()，
//    確保使用者按 X 關閉分頁時，"closed" 日誌依然能突破瀏覽器限制發送給伺服器。
// ==============================================================================
const MemoController = {
    memoId: 0,
    originalContent: '',
    isEditing: false,
    bypassWarning: false,
    channel: null,

    init: function (id, isLockedByMe) {
        this.memoId = id;
        this.channel = new BroadcastChannel('memo_sync_' + id);
        this.bindEvents();
        
        const hasIntentToEdit = sessionStorage.getItem('intentToEdit_' + id) === 'true';

        // 情況 A：F5 重整後自動恢復編輯模式
        if (isLockedByMe && hasIntentToEdit) {
            this.isEditing = true;
            this.originalContent = document.querySelector('input[name="title"]').value;
            this.toggleUI(true);
            
            // 【日誌變更】：通知後端這是一個重整，請後端把剛剛的 closed 換掉！
            this.sendLog("refreshed"); 
        }
    },

    events: {
        "beforeunload": function (e) {
            if (MemoController.isEditing && !MemoController.bypassWarning && MemoController.isContentDirty()) {
                e.preventDefault();
                e.returnValue = ''; 
            }
        },
        "pagehide": function (e) {
            // 情況 B：離開網頁 (可能是關閉，也可能是重整的第一步)
            if (MemoController.isEditing && !MemoController.bypassWarning) {
                // 一律先發送 closed，如果是重整，等下載入後端會自己修正
                MemoController.sendLog("closed", true);
            }
        },
        "message": function (event) {
            if (event.data === 'WHO_IS_EDITING' && MemoController.isEditing) {
                MemoController.channel.postMessage('I_AM_EDITING');
            }
        }
    },

    bindEvents: function () {
        window.addEventListener('beforeunload', this.events["beforeunload"]);
        window.addEventListener('pagehide', this.events["pagehide"]);
        this.channel.addEventListener('message', this.events["message"]);
    },

    sendLog: function(actionName, useBeacon = false) {
        const url = `/Memo/RecordLog/${this.memoId}?actionType=${actionName}`;
        if (useBeacon) {
            navigator.sendBeacon(url, new FormData());
        } else {
            fetch(url, { method: 'POST' }).catch(err => console.error("Log failed", err));
        }
    },

    enterEditMode: async function (id, force = false) {
        let isEditingElsewhere = false;
        const onReply = (event) => { if (event.data === 'I_AM_EDITING') isEditingElsewhere = true; };
        this.channel.addEventListener('message', onReply);
        this.channel.postMessage('WHO_IS_EDITING');
        
        await new Promise(resolve => setTimeout(resolve, 50));
        this.channel.removeEventListener('message', onReply);

        if (isEditingElsewhere) {
            alert("⚠️ 警告：您已經在另一個分頁開啟了此訂單的編輯畫面！");
            return; 
        }

        const response = await fetch(`/Memo/TryLock/${id}?force=${force}`, { method: 'POST' });
        const result = await response.json();

        if (result.success) {
            this.toggleUI(true);
            this.isEditing = true;
            this.bypassWarning = false;
            this.originalContent = document.querySelector('input[name="title"]').value;
            sessionStorage.setItem('intentToEdit_' + id, 'true');

            // 情況 C：手動點擊進入編輯
            // 【日誌】：這才是真正的 open
            this.sendLog("open");

        } else if (result.requireConfirm) {
            if (confirm(result.message)) this.enterEditMode(id, true);
        } else {
            alert(result.message);
        }
    },

    cancelEditMode: async function (id) {
        this.bypassWarning = true;
        this.isEditing = false;
        sessionStorage.removeItem('intentToEdit_' + id);

        await fetch(`/Memo/Unlock/${id}`, { method: 'POST' });
        
        document.querySelector('input[name="title"]').value = this.originalContent;
        this.toggleUI(false);

        // 情況 D：手動取消編輯
        this.sendLog("closed");
    },

    allowLeave: function () {
        // 情況 E：手動儲存
        this.bypassWarning = true;
        sessionStorage.removeItem('intentToEdit_' + this.memoId);
        // "saved" 日誌由 Controller 寫入
    },

    toggleUI: function(isEdit) {
        const view = document.getElementById('view-mode');
        const edit = document.getElementById('edit-mode');
        const btnE = document.getElementById('btn-edit');
        const btnC = document.getElementById('btn-cancel');

        if(isEdit) {
            view.classList.add('d-none'); edit.classList.remove('d-none');
            btnE.classList.add('d-none'); btnC.classList.remove('d-none');
        } else {
            view.classList.remove('d-none'); edit.classList.add('d-none');
            btnE.classList.remove('d-none'); btnC.classList.add('d-none');
        }
    },

    isContentDirty: function () {
        return document.querySelector('input[name="title"]').value !== this.originalContent;
    }
};