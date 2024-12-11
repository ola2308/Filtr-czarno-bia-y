.data
    align 16
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0   ; wagi RGB do konwersji na skalê szaroœci
    rounding_const REAL4 0.5, 0.5, 0.5, 0.5       ; sta³a do zaokr¹glania
    max_value REAL4 255.0, 255.0, 255.0, 255.0    ; maksymalna wartoœæ piksela
    min_value REAL4 0.0, 0.0, 0.0, 0.0            ; minimalna wartoœæ piksela

.code
GrayscaleFilter PROC
    ; Parametry:
    ; rcx - inputBuffer
    ; rdx - outputBuffer
    ; r8d - pixelCount
    ; xmm3 - brightness (float)

    push rbp
    mov rbp, rsp
    push rsi
    push rdi
    push rbx

    mov rsi, rcx        ; Ÿród³owy bufor
    mov rdi, rdx        ; docelowy bufor
    mov ebx, r8d        ; liczba pikseli

    ; Przygotowanie wag pomno¿onych przez jasnoœæ
    movss xmm0, dword ptr [rgb_weights]      ; waga R (0.299)
    mulss xmm0, xmm3                         ; mno¿ymy przez jasnoœæ
    movss xmm4, xmm0                         ; zachowujemy wagê R

    movss xmm0, dword ptr [rgb_weights+4]    ; waga G (0.587)
    mulss xmm0, xmm3                         ; mno¿ymy przez jasnoœæ
    movss xmm5, xmm0                         ; zachowujemy wagê G

    movss xmm0, dword ptr [rgb_weights+8]    ; waga B (0.114)
    mulss xmm0, xmm3                         ; mno¿ymy przez jasnoœæ
    movss xmm6, xmm0                         ; zachowujemy wagê B

    ; Przygotowanie licznika bloków 4-pikselowych
    mov ecx, ebx
    shr ecx, 2                               ; dzielimy przez 4
    test ecx, ecx
    jz process_remaining                     ; jeœli nie ma bloków 4-pikselowych, przechodzimy do pozosta³ych

process_4_pixels:
    push rcx                                 ; zapisujemy licznik g³ównej pêtli

    ; Przetwarzamy 4 piksele w pêtli
    mov ecx, 4                               ; licznik wewnêtrznej pêtli (4 piksele)
pixel_loop:
    ; Wczytujemy kolory w kolejnoœci BGR
    movzx eax, byte ptr [rsi]               ; B
    cvtsi2ss xmm1, eax                      ; konwertujemy na float
    mulss xmm1, xmm6                        ; mno¿ymy przez wagê B

    movzx eax, byte ptr [rsi+1]             ; G
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm5                        ; mno¿ymy przez wagê G
    addss xmm1, xmm2                        ; dodajemy do sumy

    movzx eax, byte ptr [rsi+2]             ; R
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm4                        ; mno¿ymy przez wagê R
    addss xmm1, xmm2                        ; dodajemy do sumy

    ; Zaokr¹glanie i kontrola zakresu
    addss xmm1, dword ptr [rounding_const]  ; dodajemy 0.5 do zaokr¹glenia
    maxss xmm1, dword ptr [min_value]       ; minimum 0
    minss xmm1, dword ptr [max_value]       ; maximum 255
    
    ; Konwersja na byte
    cvtss2si eax, xmm1                      ; konwertujemy float na int

    ; Zapisujemy tê sam¹ wartoœæ dla BGR
    mov byte ptr [rdi], al                  ; B
    mov byte ptr [rdi+1], al                ; G
    mov byte ptr [rdi+2], al                ; R

    ; Przesuwamy wskaŸniki na nastêpny piksel
    add rsi, 3
    add rdi, 3

    dec ecx                                 ; zmniejszamy licznik pikseli
    jnz pixel_loop                          ; jeœli nie zero, kontynuujemy pêtlê

    pop rcx                                 ; przywracamy licznik g³ównej pêtli
    dec ecx                                 ; zmniejszamy licznik bloków
    jnz process_4_pixels                    ; jeœli nie zero, przetwarzamy nastêpny blok

process_remaining:
    ; Sprawdzamy czy zosta³y jakieœ piksele
    and ebx, 3                              ; ebx mod 4
    jz cleanup                              ; jeœli nie ma pozosta³ych pikseli, koñczymy

    ; Przetwarzamy pozosta³e piksele
remaining_loop:
    ; Ten sam kod co dla pojedynczego piksela wy¿ej
    movzx eax, byte ptr [rsi]               ; B
    cvtsi2ss xmm1, eax
    mulss xmm1, xmm6

    movzx eax, byte ptr [rsi+1]             ; G
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm5
    addss xmm1, xmm2

    movzx eax, byte ptr [rsi+2]             ; R
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm4
    addss xmm1, xmm2

    addss xmm1, dword ptr [rounding_const]
    maxss xmm1, dword ptr [min_value]
    minss xmm1, dword ptr [max_value]
    
    cvtss2si eax, xmm1

    mov byte ptr [rdi], al                  ; B
    mov byte ptr [rdi+1], al                ; G
    mov byte ptr [rdi+2], al                ; R

    add rsi, 3
    add rdi, 3

    dec ebx
    jnz remaining_loop

cleanup:
    pop rbx
    pop rdi
    pop rsi
    pop rbp
    ret

GrayscaleFilter ENDP
END