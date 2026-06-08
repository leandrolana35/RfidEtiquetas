# AfixRFID — Etiquetas RFID (v2.0)

Aplicação web para **criar modelos, imprimir e gravar tags RFID** na impressora **Sato C4LX**.

Roda no seu navegador (`http://localhost:5050`), mas o programa fica no **seu computador** —
é ele que conversa com a impressora.

---

## ✅ O que tem de novo na v2.0

- **Correção do bug de gravação RFID** com texto + números acima de 14 caracteres
- Escolha do **tamanho do EPC**: 96, 128, 192 ou 256 bits
- Escolha do **banco de memória**: EPC ou User Memory
- **Duas formas de conectar** na impressora:
  - 🔌 **USB** (porta COM)
  - 🌐 **Rede** (cabo Ethernet — recomendado, mais estável)
- Botão **"Testar conexão"**
- Interface moderna no navegador

---

## 🖥️ Como instalar e rodar (passo a passo)

> ⚠️ A impressora precisa estar **no mesmo computador ou na mesma rede** onde o programa roda.
> Um programa "na nuvem" **não consegue** enxergar uma impressora USB na sua mesa.

### 1. Instalar o .NET 8 SDK (só na primeira vez)

Baixe e instale o **SDK** (não só o Runtime):
👉 https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Baixar o programa do GitHub

**Opção A — pelo site (mais fácil):**
1. Abra o repositório no GitHub
2. Botão verde **`Code`** → **`Download ZIP`**
3. Extraia a pasta no seu computador

**Opção B — pelo Git:**
```
git clone https://github.com/SEU-USUARIO/RfidEtiquetas.git
```

### 3. Executar

Dê **dois cliques** em **`INSTALAR_E_EXECUTAR.bat`**.

Na primeira vez ele baixa o necessário e compila (demora um pouco).
Depois é só abrir o navegador em **http://localhost:5050**.

---

## ⚙️ Configurar a impressora

1. Abra o programa → menu **Configurações**
2. Escolha **Rede** ou **USB**:
   - **Rede:** digite o **IP da impressora** (aparece no painel da Sato) e deixe a porta **9100**
   - **USB:** escolha a **porta COM** na lista
3. Clique em **Testar conexão**
4. Clique em **Salvar configurações**

---

## 📋 Como usar

1. **Modelos** → crie um modelo de etiqueta (textos, código de barras, RFID)
2. **Imprimir** → escolha o modelo, digite o código/dado e clique em **Imprimir**

---

## 🔧 Tecnologia

- .NET 8 (Blazor Server)
- Banco local SQLite (`Banco.db`)
- Comunicação Sato via **SBPL** (porta COM ou TCP 9100)
- Geração de código de barras com ZXing
