# API AfixRFID — Referência de Integração

Três formas de integrar sistemas externos: **API REST**, **Webhooks** e um **servidor MCP** (para agentes de IA). As três usam a mesma autenticação.

URL base: o endereço onde o app **Local** está publicado (ex: `http://localhost:5090` em desenvolvimento).

## Autenticação

Toda requisição leva o cabeçalho `X-API-Key`. Uma chave pertence a uma empresa — recursos de outra empresa retornam `403`. Gere uma chave em **Chaves de API** no painel (Administrador); o valor só é mostrado uma vez, na criação.

```bash
curl {base_url}/api/v1/printers -H "X-API-Key: afx_sua_chave_aqui"
```

## API REST

### `POST /api/v1/print`

Coloca um job na fila de impressão (assíncrono). Retorna `jobId` para consultar depois.

| Campo | Tipo | | Descrição |
|---|---|---|---|
| `epc` | string | obrigatório | Código a gravar no chip RFID |
| `estacaoId` | int | obrigatório | Id da estação/impressora — ver `GET /printers` |
| `layoutId` | int | obrigatório | Id do modelo de etiqueta |
| `sku` | string | opcional | Código do produto |
| `lote` | string | opcional | Identificador de lote livre |
| `projetoId` | int | opcional | Liga o job a um Projeto cadastrado (exige `numeroSequencia`) |
| `numeroSequencia` | long | opcional | Número da faixa do projeto (exige `projetoId`) |

```bash
curl -X POST {base_url}/api/v1/print \
  -H "X-API-Key: afx_sua_chave_aqui" -H "Content-Type: application/json" \
  -d '{"epc":"E28011700000020DAB1B2C3D","sku":"SKU-1001","estacaoId":1,"layoutId":2}'
```

Resposta `202`:
```json
{ "jobId": "5671ac90-68fa-4620-beb2-2e5f7bc7d399", "status": "pendente", "criadoEm": "2026-07-31T19:41:07.17Z" }
```

### `GET /api/v1/jobs/{jobId}`

`status`: `pendente` | `processando` | `concluido` | `falhou`. Job de outra empresa (ou inexistente) → `404`.

```json
{
  "jobId": "5671ac90-68fa-4620-beb2-2e5f7bc7d399", "status": "concluido",
  "epc": "E28011700000020DAB1B2C3D", "sku": "SKU-1001", "lote": null,
  "estacao": "Sato CL4NX", "layout": "AFIX4318_Offmetal", "projetoId": null,
  "erro": null, "criadoEm": "2026-07-31T19:41:07.17Z", "processadoEm": "2026-07-31T19:41:08.00Z"
}
```

### `GET /api/v1/printers`

Lista as estações da empresa, testando a conexão na hora.

```json
[{ "id": 1, "nome": "Sato CL4NX", "modelo": "Sato_CL4NX", "online": true, "erro": null }]
```

### Erros

| Código | Quando |
|---|---|
| `401` | `X-API-Key` ausente ou inválida/revogada |
| `400` | Campo obrigatório ausente, ou `estacaoId`/`layoutId`/`projetoId` não encontrado |
| `403` | O recurso existe, mas é de outra empresa |
| `404` | Job inexistente ou de outra empresa |

Corpo de erro: `{ "erro": "mensagem" }` (exceto 400 de schema, que usa o formato padrão do ASP.NET Core).

## Webhooks

**Cadastro:** hoje é manual, feito por um Administrador na tela **Webhooks** — URL de destino + eventos desejados. O *secret* é gerado automaticamente.

**Eventos:** `job.created` (ao aceitar o POST /print), `job.completed` / `job.failed` (ao terminar de processar).

```json
// job.created
{ "evento": "job.created", "jobId": "...", "epc": "...", "sku": "...", "lote": null,
  "estacaoId": 1, "layoutId": 2, "projetoId": null, "criadoEm": "..." }

// job.completed / job.failed
{ "evento": "job.completed", "jobId": "...", "epc": "...", "sku": "...",
  "status": "concluido", "erro": null, "processadoEm": "..." }
```

**Cabeçalhos de cada entrega:** `X-Webhook-Event`, `X-Webhook-Id`, `X-Webhook-Signature: sha256=<hex>` — HMAC-SHA256 do corpo bruto (bytes exatos recebidos) usando o secret.

```js
// Node.js
const crypto = require("crypto");
function assinaturaValida(corpoBruto, assinaturaRecebida, secret) {
  const esperada = "sha256=" + crypto.createHmac("sha256", secret).update(corpoBruto).digest("hex");
  return crypto.timingSafeEqual(Buffer.from(esperada), Buffer.from(assinaturaRecebida));
}
```

```python
# Python
import hmac, hashlib
def assinatura_valida(corpo_bruto: bytes, assinatura_recebida: str, secret: str) -> bool:
    esperada = "sha256=" + hmac.new(secret.encode(), corpo_bruto, hashlib.sha256).hexdigest()
    return hmac.compare_digest(esperada, assinatura_recebida)
```

**Retentativas:** backoff exponencial `30s → 2min → 10min → 30min → 2h`; depois disso, fica marcada como **esgotada** e não é reenviada. Histórico visível na tela Webhooks.

## Servidor MCP

Endpoint `POST {base_url}/mcp`, mesma autenticação `X-API-Key`, transporte HTTP (Streamable HTTP/SSE). Handshake padrão: `initialize` → `tools/list` → `tools/call`.

### `print_rfid_tag`

Equivalente a `POST /api/v1/print`. `label_data` é uma *string* JSON.

| Parâmetro | Descrição |
|---|---|
| `epc` | Código a gravar |
| `printer_id` | Id da estação de destino |
| `label_data` | JSON com `layoutId` (obrigatório), `sku`/`lote`/`projetoId`/`numeroSequencia` (opcionais) |

```json
{ "epc": "E28011...", "printer_id": 1, "label_data": "{\"layoutId\":2,\"sku\":\"SKU-1001\"}" }
```

### `check_printer_status`

Parâmetro: `printer_id`. Testa a conexão de verdade.

### `search_printed_tags`

Histórico de jobs criados via API/MCP. Parâmetros opcionais: `epc`, `sku` (busca parcial), `status`, `limit` (padrão 50, máx. 200).

### Exemplo completo

```bash
curl -X POST {base_url}/mcp \
  -H "X-API-Key: afx_sua_chave_aqui" -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"check_printer_status","arguments":{"printer_id":1}}}'
```
