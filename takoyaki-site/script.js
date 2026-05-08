const menuData = {
  classic: {
    tag: "初手はこれでいい",
    name: "極みソース",
    desc:
      "旨味の濃い自家製ソース、薄削り節、青のり。甘さを抑え、生地の出汁を殺さない配合にしている。",
    small: 580,
    large: 900,
    pair: "黒烏龍茶",
  },
  salt: {
    tag: "生地の差が出る",
    name: "焦がし塩",
    desc:
      "藻塩、白ごま油、焼き海苔。ソースを外すぶん、蛸と出汁の輪郭がはっきり出る。",
    small: 620,
    large: 960,
    pair: "冷たい煎茶",
  },
  dashi: {
    tag: "夜の締め向け",
    name: "出汁浸し",
    desc:
      "焼きたてを追い出汁に沈める。明石焼きに近いが、表面の香ばしさは残す。",
    small: 680,
    large: 1050,
    pair: "山椒ハイボール",
  },
};

const header = document.querySelector("[data-header]");
const tabs = document.querySelectorAll("[data-menu]");
const menuTag = document.querySelector("[data-menu-tag]");
const menuName = document.querySelector("[data-menu-name]");
const menuDesc = document.querySelector("[data-menu-desc]");
const menuSmall = document.querySelector("[data-menu-small]");
const menuLarge = document.querySelector("[data-menu-large]");
const menuPair = document.querySelector("[data-menu-pair]");
const form = document.querySelector("[data-order-form]");
const flavor = document.querySelector("[data-flavor]");
const quantity = document.querySelector("[data-quantity]");
const pickup = document.querySelector("[data-pickup]");
const result = document.querySelector("[data-order-result]");

function yen(value) {
  return `${value.toLocaleString("ja-JP")}円`;
}

function setMenu(key) {
  const item = menuData[key];
  if (!item) return;

  menuTag.textContent = item.tag;
  menuName.textContent = item.name;
  menuDesc.textContent = item.desc;
  menuSmall.textContent = yen(item.small);
  menuLarge.textContent = yen(item.large);
  menuPair.textContent = item.pair;

  tabs.forEach((tab) => {
    const active = tab.dataset.menu === key;
    tab.classList.toggle("active", active);
    tab.setAttribute("aria-selected", String(active));
  });
}

function calculatePrice(key, count) {
  const item = menuData[key];
  if (count === 6) return item.small;
  if (count === 10) return item.large;
  return item.large + item.small + 120;
}

function startTime(timeValue) {
  const [hour, minute] = timeValue.split(":").map(Number);
  const date = new Date(2026, 0, 1, hour, minute);
  date.setMinutes(date.getMinutes() - 8);
  return date.toLocaleTimeString("ja-JP", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  });
}

function updateOrder() {
  const key = flavor.value;
  const count = Number(quantity.value);
  const item = menuData[key];
  const price = calculatePrice(key, count);
  result.textContent = `${item.name} ${count}個、${yen(price)}。${pickup.value} 受取なら、${startTime(
    pickup.value
  )} から焼き始める。`;
}

tabs.forEach((tab) => {
  tab.addEventListener("click", () => {
    setMenu(tab.dataset.menu);
    flavor.value = tab.dataset.menu;
    updateOrder();
  });
});

form.addEventListener("submit", (event) => {
  event.preventDefault();
  setMenu(flavor.value);
  updateOrder();
});

[flavor, quantity, pickup].forEach((input) => {
  input.addEventListener("change", updateOrder);
});

window.addEventListener("scroll", () => {
  header.classList.toggle("scrolled", window.scrollY > 18);
});

setMenu("classic");
updateOrder();
