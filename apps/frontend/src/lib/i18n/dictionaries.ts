// All customer-facing text. Menu item names and descriptions come from the API and are shown as entered.
// Khmer text should be reviewed by a native speaker before launch.

export type Locale = "en" | "km";

export const LOCALES: readonly Locale[] = ["en", "km"];
export const DEFAULT_LOCALE: Locale = "en";
export const LOCALE_COOKIE_NAME = "lang";

export function isLocale(value: string | undefined | null): value is Locale {
  return value === "en" || value === "km";
}

const en = {
  "app.table": "Table {number}",
  "app.language": "Language",
  "app.retry": "Try again",
  "app.close": "Close",
  "app.skipToContent": "Skip to menu",
  "menu.categories": "Menu categories",
  "menu.soldOut": "Sold out",
  "menu.empty": "This menu has no dishes yet.",
  "menu.emptyCategory": "No dishes in this category yet.",
  "menu.loadError": "We could not load the menu",
  "menu.loadErrorHint": "Check your connection and try again.",
  "product.required": "Required",
  "product.optional": "Optional",
  "product.chooseOne": "Choose 1",
  "product.chooseUpTo": "Choose up to {max}",
  "product.chooseAtLeast": "Choose at least {min}",
  "product.chooseBetween": "Choose {min} to {max}",
  "product.chooseExactly": "Choose {count}",
  "product.quantity": "Quantity",
  "product.increase": "Add one more",
  "product.decrease": "Remove one",
  "product.notes": "Note for the kitchen",
  "product.notesPlaceholder": "For example: no ice",
  "product.add": "Add to cart",
  "product.added": "Added to cart",
  "cart.title": "Your cart",
  "cart.view": "View cart ({count})",
  "cart.empty": "Your cart is empty",
  "cart.emptyHint": "Pick something from the menu to start.",
  "cart.remove": "Remove {name}",
  "cart.total": "Total",
  "cart.estimate": "Estimate. The final price is confirmed when you order.",
  "cart.pricing": "Checking prices…",
  "cart.priceError": "We could not check the prices",
  "cart.fixItems": "Please fix your cart",
  "cart.notes": "Note for the kitchen (optional)",
  "cart.table": "Your table",
  "cart.chooseTable": "Choose your table",
  "cart.tableRequired": "Please choose your table number.",
  "cart.tablesError": "We could not load the tables",
  "cart.placeOrder": "Place order",
  "cart.placing": "Placing your order…",
  "cart.orderFailed": "We could not place your order",
  "cart.stockChanged": "Stock just changed. Please check your cart and try again.",
  "cart.tableNotFound": "That table was not found. Please choose another table.",
  "order.title": "Your order",
  "order.myOrder": "My order",
  "order.table": "Table {number}",
  "order.status.PENDING": "Order received",
  "order.status.COOKING": "Cooking",
  "order.status.READY": "Ready",
  "order.status.COMPLETED": "Completed",
  "order.status.CANCELLED": "Cancelled",
  "order.hint.PENDING": "The kitchen has your order.",
  "order.hint.COOKING": "The kitchen is preparing your food.",
  "order.hint.READY": "Your food is ready.",
  "order.hint.COMPLETED": "Thank you. Enjoy your meal!",
  "order.hint.CANCELLED": "This order was cancelled. Please ask a staff member.",
  "order.items": "Items",
  "order.total": "Total",
  "order.autoRefresh": "This page updates by itself.",
  "order.notFound": "We could not find this order",
  "order.loadError": "We could not load your order status",
  "order.backToMenu": "Back to the menu",
  "error.title": "Something went wrong",
  "error.hint": "Please try again.",
} as const;

export type MessageKey = keyof typeof en;
export type Dictionary = Record<MessageKey, string>;

const km: Dictionary = {
  "app.table": "តុ {number}",
  "app.language": "ភាសា",
  "app.retry": "ព្យាយាមម្តងទៀត",
  "app.close": "បិទ",
  "app.skipToContent": "រំលងទៅម៉ឺនុយ",
  "menu.categories": "ប្រភេទម្ហូប",
  "menu.soldOut": "អស់ពីស្តុក",
  "menu.empty": "ម៉ឺនុយនេះមិនទាន់មានមុខម្ហូបនៅឡើយទេ។",
  "menu.emptyCategory": "ប្រភេទនេះមិនទាន់មានមុខម្ហូបនៅឡើយទេ។",
  "menu.loadError": "យើងមិនអាចផ្ទុកម៉ឺនុយបានទេ",
  "menu.loadErrorHint": "សូមពិនិត្យអ៊ីនធឺណិតរបស់អ្នក ហើយព្យាយាមម្តងទៀត។",
  "product.required": "ត្រូវជ្រើសរើស",
  "product.optional": "ជម្រើស",
  "product.chooseOne": "ជ្រើសរើស ១",
  "product.chooseUpTo": "ជ្រើសរើសបានរហូតដល់ {max}",
  "product.chooseAtLeast": "ជ្រើសរើសយ៉ាងហោចណាស់ {min}",
  "product.chooseBetween": "ជ្រើសរើស {min} ដល់ {max}",
  "product.chooseExactly": "ជ្រើសរើស {count}",
  "product.quantity": "ចំនួន",
  "product.increase": "បន្ថែមមួយទៀត",
  "product.decrease": "ដកមួយចេញ",
  "product.notes": "កំណត់ចំណាំជូនផ្ទះបាយ",
  "product.notesPlaceholder": "ឧទាហរណ៍៖ មិនដាក់ទឹកកក",
  "product.add": "បន្ថែមទៅកន្ត្រក",
  "product.added": "បានបន្ថែមទៅកន្ត្រក",
  "cart.title": "កន្ត្រករបស់អ្នក",
  "cart.view": "មើលកន្ត្រក ({count})",
  "cart.empty": "កន្ត្រករបស់អ្នកនៅទទេ",
  "cart.emptyHint": "ជ្រើសរើសមុខម្ហូបពីម៉ឺនុយដើម្បីចាប់ផ្តើម។",
  "cart.remove": "ដក {name} ចេញ",
  "cart.total": "សរុប",
  "cart.estimate": "តម្លៃប៉ាន់ស្មាន។ តម្លៃចុងក្រោយនឹងបញ្ជាក់ពេលអ្នកកម្មង់។",
  "cart.pricing": "កំពុងពិនិត្យតម្លៃ…",
  "cart.priceError": "យើងមិនអាចពិនិត្យតម្លៃបានទេ",
  "cart.fixItems": "សូមកែតម្រូវកន្ត្រករបស់អ្នក",
  "cart.notes": "កំណត់ចំណាំជូនផ្ទះបាយ (មិនបង្ខំ)",
  "cart.table": "តុរបស់អ្នក",
  "cart.chooseTable": "ជ្រើសរើសតុរបស់អ្នក",
  "cart.tableRequired": "សូមជ្រើសរើសលេខតុរបស់អ្នក។",
  "cart.tablesError": "យើងមិនអាចផ្ទុកបញ្ជីតុបានទេ",
  "cart.placeOrder": "ដាក់ការកម្មង់",
  "cart.placing": "កំពុងដាក់ការកម្មង់…",
  "cart.orderFailed": "យើងមិនអាចដាក់ការកម្មង់របស់អ្នកបានទេ",
  "cart.stockChanged": "ស្តុកទើបតែផ្លាស់ប្តូរ។ សូមពិនិត្យកន្ត្រករបស់អ្នក ហើយព្យាយាមម្តងទៀត។",
  "cart.tableNotFound": "រកមិនឃើញតុនោះទេ។ សូមជ្រើសរើសតុផ្សេង។",
  "order.title": "ការកម្មង់របស់អ្នក",
  "order.myOrder": "ការកម្មង់របស់ខ្ញុំ",
  "order.table": "តុ {number}",
  "order.status.PENDING": "បានទទួលការកម្មង់",
  "order.status.COOKING": "កំពុងធ្វើម្ហូប",
  "order.status.READY": "រួចរាល់ហើយ",
  "order.status.COMPLETED": "បានបញ្ចប់",
  "order.status.CANCELLED": "បានលុបចោល",
  "order.hint.PENDING": "ផ្ទះបាយបានទទួលការកម្មង់របស់អ្នកហើយ។",
  "order.hint.COOKING": "ផ្ទះបាយកំពុងរៀបចំអាហាររបស់អ្នក។",
  "order.hint.READY": "អាហាររបស់អ្នករួចរាល់ហើយ។",
  "order.hint.COMPLETED": "សូមអរគុណ។ សូមរីករាយជាមួយអាហារ!",
  "order.hint.CANCELLED": "ការកម្មង់នេះត្រូវបានលុបចោល។ សូមសួរបុគ្គលិក។",
  "order.items": "មុខម្ហូប",
  "order.total": "សរុប",
  "order.autoRefresh": "ទំព័រនេះធ្វើបច្ចុប្បន្នភាពដោយខ្លួនឯង។",
  "order.notFound": "យើងរកមិនឃើញការកម្មង់នេះទេ",
  "order.loadError": "យើងមិនអាចផ្ទុកស្ថានភាពការកម្មង់របស់អ្នកបានទេ",
  "order.backToMenu": "ត្រឡប់ទៅម៉ឺនុយ",
  "error.title": "មានបញ្ហាកើតឡើង",
  "error.hint": "សូមព្យាយាមម្តងទៀត។",
};

export const dictionaries: Record<Locale, Dictionary> = { en, km };

export type TranslateFn = (key: MessageKey, values?: Record<string, string | number>) => string;

export function createTranslator(locale: Locale): TranslateFn {
  const dictionary = dictionaries[locale];

  return (key, values) => {
    const template = dictionary[key];
    if (!values) {
      return template;
    }
    return template.replace(/\{(\w+)\}/g, (match, name: string) =>
      name in values ? String(values[name]) : match,
    );
  };
}
