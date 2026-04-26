/**
 * ==============================================================================
 * 最終精簡版 memo-details.js
 * 實作功能：
 * 1. 點擊按鈕才觸發廣播檢查，避免重複編輯分頁。
 * 2. 移除網址 Hash 依賴，純內部狀態驅動。
 * 3. 自動化日誌結算 (Start/Finish Log)。
 * ==============================================================================
 */

class MemoController {
  #memoId = 0;
  #originalContent = "";
  #isEditing = false;
  #bypassWarning = false;
  #channel = null;
  #logId = null;

  #urls = {
    tryLock: "/Memo/TryLock",
    unlock: "/Memo/Unlock"
  };

  init(id) {
    this.#memoId = id;
    this.#channel = new BroadcastChannel("memo_sync_" + id);
    this.#bindEvents();

    // 初始狀態一律為檢視模式
    this.#isEditing = false;
    this.#toggleUI(false);

    // 從 Session 拿回可能存在的 logId (用於重整後的狀態追蹤)
    this.#logId = sessionStorage.getItem("activeLogId_" + this.#memoId);
  }

  allowLeave() {
    this.#bypassWarning = true;
  }

  /**
   * 公開介面：供按鈕呼叫
   */
  startEdit() {
    this.#enterEditMode(false);
  }

  cancelEdit() {
    this.#cancelEditMode();
  }

  /**
   * 核心邏輯：進入編輯模式 (包含跨分頁檢查)
   */
  async #enterEditMode(force = false) {
    let isEditingElsewhere = false;
    const onReply = (event) => {
      // 避免重複編輯分頁：標記已存在其他編輯者
      if (event.data === "I_AM_EDITING") isEditingElsewhere = true;
    };

    this.#channel.addEventListener("message", onReply);
    this.#channel.postMessage("WHO_IS_EDITING");

    // 等待其他分頁回應
    await new Promise((resolve) => setTimeout(resolve, 50));
    
    // 5. 【關鍵點 B】：不論結果如何，必須「移除」監聽器 👈 這是避免第二次失效的主因
    this.#channel.removeEventListener("message", onReply);

    if (isEditingElsewhere) {
      // 避免重複編輯分頁：若發現其他分頁正在編輯，則阻斷進入並警示
      alert("⚠️ 注意：您已在另一個分頁開啟此單據的編輯畫面！\n\n此分頁將維持在「檢視模式」。");
      this.#toggleUI(false);
      return;
    }

    // 嘗試向後端取得資料庫鎖定
    const response = await fetch(`${this.#urls.tryLock}/${this.#memoId}?force=${force}`, { method: "POST" });
    const result = await response.json();

    if (result.success) {
      this.#toggleUI(true);
      this.#isEditing = true;

      // 若 Session 無紀錄，代表是全新開啟的編輯 Session，需建立 Log
      if (!this.#logId) {
        try {
          const res = await fetch(`/Log/StartLog/${this.#memoId}`, { method: "POST" });
          const data = await res.json();
          this.#logId = data.logId;
          sessionStorage.setItem("activeLogId_" + this.#memoId, this.#logId);
        } catch (e) {
          console.error("Log 建立失敗:", e);
        }
      }
    } else {
      alert(result.message);
    }
  }

  async #cancelEditMode() {
    this.#bypassWarning = true;
    this.#isEditing = false;

    await this.#finishLog("closed");
    await fetch(`${this.#urls.unlock}/${this.#memoId}`, { method: "POST" });
    this.#toggleUI(false);
  }

  async #finishLog(actionType) {
    if (!this.#logId) return;
    await fetch(`/Log/FinishLog?logId=${this.#logId}&actionType=${actionType}`, { method: "POST" });
    sessionStorage.removeItem("activeLogId_" + this.#memoId);
    this.#logId = null;
  }

  #toggleUI(isEdit) {
    const view = document.getElementById("view-mode");
    const edit = document.getElementById("edit-mode");
    const btnE = document.getElementById("btn-edit");
    const btnC = document.getElementById("btn-cancel");
    
    // ✅ 建議：直接用 ID 抓取輸入框，比用 name 屬性更準確
    const titleInput = document.getElementById("memo-title-input"); 
    const viewTitle = document.getElementById("view-title-text");

    if (isEdit) {
      view.classList.add("d-none");
      edit.classList.remove("d-none");
      btnE.classList.add("d-none");
      btnC.classList.remove("d-none");
      
      // 進入編輯時，先存下目前的內容
      if (titleInput) {
          this.#originalContent = titleInput.value;
      }
    } else {
      view.classList.remove("d-none");
      edit.classList.add("d-none");
      btnE.classList.remove("d-none");
      btnC.classList.add("d-none");
      
      // ✅ 修正點：取消編輯時，把內容還原成「原始內容」
      if (titleInput) {
          titleInput.value = this.#originalContent;
      }
    }
  }

  #isContentDirty() {
    const input = document.querySelector('input[name="title"]');
    return input && input.value !== this.#originalContent;
  }

  #bindEvents() {
    window.addEventListener("beforeunload", this.#onBeforeUnload);
    window.addEventListener("pagehide", this.#onPageHide);
    this.#channel.addEventListener("message", this.#onMessage);
  }

  #onBeforeUnload = (e) => {
    if (this.#isEditing && !this.#bypassWarning && this.#isContentDirty()) {
      e.preventDefault();
      e.returnValue = "";
    }
  };

  #onPageHide = () => {
    if (this.#isEditing && !this.#bypassWarning && this.#logId) {
      // 視窗關閉時利用 sendBeacon 強制結算 Log
      const url = `/Log/FinishLog?logId=${this.#logId}&actionType=closed`;
      navigator.sendBeacon(url);
    }
  };

  #onMessage = (event) => {
    // 避免重複編輯分頁：若別的分頁在詢問，且我正在編輯，則回應「我在這裡」
    if (event.data === "WHO_IS_EDITING" && this.#isEditing) {
      this.#channel.postMessage("I_AM_EDITING");
    }
  };
}

// 建立實例供頁面使用
const memoController = new MemoController();