const MemoController = {
    // --- 狀態變數 ---
    memoId: 0,
    originalContent: '',
    isEditing: false,
    bypassWarning: false,

    // --- 初始化入口 ---
    init: function (id, isLockedByMe) {
        this.memoId = id;
        this.bindEvents();
        
        // 【核心解法：雙重驗證】
        // 1. 去前端記憶體找找看，有沒有留下「我想編輯」的記號？
        const hasIntentToEdit = sessionStorage.getItem('intentToEdit_' + id) === 'true';

        // 2. 只有在「後端說鎖是我的」且「前端記憶說我想編輯 (代表是F5)」時，才自動切換！
        if (isLockedByMe && hasIntentToEdit) {
            this.isEditing = true;
            this.originalContent = document.querySelector('input[name="title"]').value;
            this.toggleUI(true);
        } else {
            // 如果 isLockedByMe 是 true，但 hasIntentToEdit 是 false
            // 代表這是「關閉分頁後重新打開」造成的幽靈鎖，我們乖乖待在「檢視模式」！
        }
    },

    // --- 事件映射表 ---
    events: {
        "beforeunload": function (e) {
            if (MemoController.isEditing && !MemoController.bypassWarning && MemoController.isContentDirty()) {
                e.preventDefault();
                e.returnValue = ''; 
            }
        }
    },

    bindEvents: function () {
        window.addEventListener('beforeunload', this.events["beforeunload"]);
    },

    // --- 核心功能方法 ---
    
    enterEditMode: async function (id, force = false) {
        const response = await fetch(`/Memo/TryLock/${id}?force=${force}`, { method: 'POST' });
        const result = await response.json();

        if (result.success) {
            this.toggleUI(true);
            this.isEditing = true;
            this.bypassWarning = false;
            this.originalContent = document.querySelector('input[name="title"]').value;
            
            // 【新增】搶鎖成功時，寫下 F5 用的記號
            sessionStorage.setItem('intentToEdit_' + id, 'true');
            
        } else if (result.requireConfirm) {
            if (confirm(result.message)) {
                this.enterEditMode(id, true);
            }
        } else {
            alert(result.message);
        }
    },

    cancelEditMode: async function (id) {
        this.bypassWarning = true;
        this.isEditing = false;

        // 【新增】取消編輯時，清除 F5 記號
        sessionStorage.removeItem('intentToEdit_' + id);

        await fetch(`/Memo/Unlock/${id}`, { method: 'POST' });
        
        document.querySelector('input[name="title"]').value = this.originalContent;
        this.toggleUI(false);
    },

    allowLeave: function () {
        this.bypassWarning = true;
        // 【新增】送出表單時，清除 F5 記號
        sessionStorage.removeItem('intentToEdit_' + this.memoId);
    },

    toggleUI: function(isEdit) {
        const view = document.getElementById('view-mode');
        const edit = document.getElementById('edit-mode');
        const btnE = document.getElementById('btn-edit');
        const btnC = document.getElementById('btn-cancel');

        if(isEdit) {
            view.classList.add('d-none');
            edit.classList.remove('d-none');
            btnE.classList.add('d-none');
            btnC.classList.remove('d-none');
        } else {
            view.classList.remove('d-none');
            edit.classList.add('d-none');
            btnE.classList.remove('d-none');
            btnC.classList.add('d-none');
        }
    },

    isContentDirty: function () {
        return document.querySelector('input[name="title"]').value !== this.originalContent;
    }
};