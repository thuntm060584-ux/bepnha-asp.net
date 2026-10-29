(() => {
  "use strict";
  const messageTimers = new WeakMap();
  function dismissAfterThreeSeconds(message) {
    clearTimeout(messageTimers.get(message));
    messageTimers.set(message, setTimeout(() => {
      message.remove();
      messageTimers.delete(message);
    }, 3000));
  }
  document.querySelectorAll("[data-auto-dismiss]").forEach(dismissAfterThreeSeconds);
  const currentPath = location.pathname.replace(/\/$/, "") || "/";
  document.querySelectorAll(".navbar-nav > a.nav-link, .admin-nav > a").forEach(link => {
    const path = new URL(link.href, location.origin).pathname;
    if (path === currentPath || (path === "/recipes" && /^\/recipes\/\d+$/.test(currentPath))) {
      link.classList.add("is-current");
      link.setAttribute("aria-current", "page");
    }
  });
  const errorText = async response => {
    if (response.status === 401) {
      location.href = "/auth/login?returnUrl=" + encodeURIComponent(location.pathname);
      return "Vui lòng đăng nhập.";
    }
    try { const body = await response.json(); return body.error || "Không thể xử lý yêu cầu. Hãy tải lại trang và thử lại."; }
    catch { return "Không thể xử lý yêu cầu. Hãy tải lại trang và thử lại."; }
  };
  document.addEventListener("submit", async event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (form.dataset.confirm && !confirm(form.dataset.confirm)) { event.preventDefault(); return; }
    if (!form.hasAttribute("data-follow") && !form.hasAttribute("data-review")) return;
    event.preventDefault();
    const button = form.querySelector("button[type=submit],button:not([type])");
    if (button) button.disabled = true;
    try {
      const response = await fetch(form.action, { method: "POST", body: new FormData(form), headers: { "X-Requested-With": "XMLHttpRequest" } });
      if (!response.ok) throw new Error(await errorText(response));
      const data = await response.json();
      if (form.hasAttribute("data-follow")) {
        document.querySelectorAll("[data-follow-button]").forEach(x => x.textContent = data.following ? "Đang theo dõi · Bỏ theo dõi" : "Theo dõi tác giả");
        document.querySelectorAll("[data-follower-count]").forEach(x => x.textContent = data.count);
      } else {
        const result = await fetch(data.redirect, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!result.ok) throw new Error("Đã lưu đánh giá. Vui lòng tải lại trang để xem.");
        const page = new DOMParser().parseFromString(await result.text(), "text/html");
        const reviews = document.querySelector("#reviews"), replacement = page.querySelector("#reviews");
        if (reviews && replacement) reviews.replaceWith(replacement);
        document.querySelector("#average-rating").textContent = Number(data.average).toFixed(1);
        document.querySelector("#review-count").textContent = data.count;
      }
    } catch (error) {
      let message = form.querySelector(".ajax-message");
      if (!message) { message = document.createElement("p"); message.className = "ajax-message text-danger mt-2"; message.setAttribute("role", "alert"); form.append(message); }
      message.textContent = error.message;
      dismissAfterThreeSeconds(message);
    } finally { if (button) button.disabled = false; }
  });
  const editor = document.querySelector("#recipe-editor");
  if (!editor) return;
  function reindex() {
    document.querySelectorAll("#ingredient-lines .dynamic-line").forEach((row, index) => {
      row.querySelectorAll("input").forEach((input, column) => {
        const field = "Ingredients[" + index + "]." + (column ? "Quantity" : "Name");
        input.name = field;
        input.removeAttribute("id");
        const message = input.parentElement.querySelector("[data-valmsg-for]");
        if (message) message.setAttribute("data-valmsg-for", field);
      });
    });
    document.querySelectorAll("#step-lines .dynamic-line").forEach((row, index) => {
      const field = "Steps[" + index + "]";
      const textarea = row.querySelector("textarea");
      textarea.name = field;
      textarea.removeAttribute("id");
      const message = row.querySelector("[data-valmsg-for]");
      if (message) message.setAttribute("data-valmsg-for", field);
      row.querySelector(".step-number").textContent = index + 1;
    });
  }
  editor.addEventListener("click", event => {
    const button = event.target.closest("button");
    if (!button) return;
    if (button.classList.contains("remove-line")) {
      const row = button.closest(".dynamic-line");
      if (row.parentElement.children.length > 1) { row.remove(); reindex(); }
      return;
    }
    const container = button.id === "add-ingredient" ? document.querySelector("#ingredient-lines") : button.id === "add-step" ? document.querySelector("#step-lines") : null;
    if (!container || container.children.length >= 100) return;
    const row = container.firstElementChild.cloneNode(true);
    row.querySelectorAll("input,textarea").forEach(x => { x.value = ""; x.classList.remove("input-validation-error"); });
    container.append(row); reindex(); row.querySelector("input,textarea").focus();
  });
  let imagePreview = editor.querySelector(".cover-preview");
  const originalPreview = imagePreview?.getAttribute("src");
  let previewUrl;
  editor.querySelector("[type=file]").addEventListener("change", event => {
    const file = event.target.files[0];
    const extension = file ? file.name.slice(file.name.lastIndexOf(".")).toLowerCase() : "";
    if (file && ![".jpg", ".png"].includes(extension)) {
      event.target.value = "";
      event.target.setCustomValidity("Chỉ nhận ảnh .JPG hoặc .PNG.");
      event.target.reportValidity();
      return;
    }
    if (file && file.size > 5 * 1024 * 1024) { event.target.setCustomValidity("Ảnh tối đa 5 MB."); event.target.reportValidity(); return; }
    event.target.setCustomValidity("");
    const caption = editor.querySelector("[data-upload-title]");
    if (caption) caption.textContent = file?.name || "Chọn ảnh món ăn của bạn";
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    if (!file) {
      if (imagePreview && originalPreview) imagePreview.src = originalPreview;
      else if (imagePreview) { imagePreview.remove(); imagePreview = null; }
      return;
    }
    if (!imagePreview) {
      imagePreview = document.createElement("img");
      imagePreview.className = "cover-preview";
      imagePreview.alt = "Xem trước ảnh món ăn đã chọn";
      event.target.closest(".upload-field").parentElement.append(imagePreview);
    }
    previewUrl = URL.createObjectURL(file);
    imagePreview.src = previewUrl;
  });
})();
