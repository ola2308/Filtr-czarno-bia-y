.data
    align 16                        ; wyr�wnanie do 16 bajt�w
    ; we liczby s�u�� do zmiany kolorowego obrazka na czarno-bia�y
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0   
    rounding_const REAL4 0.5, 0.5, 0.5, 0.5       ; liczby do zaokr�glania, �eby nie by�o brzydkich warto�ci
    max_value REAL4 255.0, 255.0, 255.0, 255.0    ; najwi�ksza jasno�� jak� mo�e mie� piksel
    min_value REAL4 0.0, 0.0, 0.0, 0.0           ; najmniejsza jasno�� jak� mo�e mie� piksel

.code
GrayscaleFilter PROC
    ;==========================================
    ; Parametry wej�ciowe (konwencja MS x64):
    ; rcx - wska�nik gdzie jest obraz
    ; rdx - wska�nik gdzie zapisa� obraz
    ; r8d - pixelCount
    ; xmm3 - jasno��
    ;==========================================

    ;robimy porz�dek w pami�ci
    push rbp ; zapisujemy gdzie teraz jeste�my, �eby p�niej umie� wr�ci�
    mov rbp, rsp ; tu b�d� nasze nowe rzeczy
    push rsi  ; zapisujemy gdzie mamy obrazek �r�d�owy
    push rdi ; zapisujemy gdzie b�dziemy dawa� nowy obrazek
    push rbx ; zapisujemy jeszcze jedno miejsce na r�ne rzeczy
    
    mov rsi, rcx  ; tu jest nasz kolorowy obrazek
    mov rdi, rdx  ; tu b�dziemy robi� czarno-bia�y obrazek
    mov ebx, r8d ; tu mamy zapisane ile pikseli musimy zmieni�
    
    ;przygotowujemy super liczby do zmiany kolor�w
    movups xmm0, [rgb_weights] ; bierzemy nasze liczby do zmiany kolor�w 
    shufps xmm3, xmm3, 0  ; kopiujemy warto�� jasno�ci wsz�dzie
    mulps xmm0, xmm3  ; mno�ymy nasze liczby przez jasno��
    
    ;robimy kopie naszych liczb bo b�dziemy ich du�o u�ywa�
    movaps xmm4, xmm0     ; kopia dla czerwonego koloru       
    movaps xmm5, xmm0     ; kopia dla zielonego koloru     
    movaps xmm6, xmm0     ; kopia dla niebieskiego koloru     
    
    
    shufps xmm4, xmm4, 0          ; kopiujemy czerwony wsz�dzie
    shufps xmm5, xmm5, 01010101b  ; kopiujemy zielony wsz�dzie
    shufps xmm6, xmm6, 10101010b  ; kopiujemy niebieski wsz�dzie
    
    movups xmm7, [max_value]     ; najbardziej bia�y kolor jaki mo�e by�
    movups xmm8, [min_value]     ; najbardziej czarny kolor
    movups xmm9, [rounding_const] ; liczby do zaokr�glania
    
    ;ile grup po 4 piksele musimy zrobi�
    mov ecx, ebx    ; bierzemy liczb� wszystkich pikseli
    shr ecx, 2      ; dzielimy przez 4 bo b�dziemy robi� po 4 na raz, zwi�zane z SHR(Shift Right) - przesuni�cie bitowe w prawo. przesuwamy bit o 2 pozycje w prawo czyli dzielimy przez 4           
    test ecx, ecx   ; sprawdzamy czy w og�le jest co robi�
    jz process_remaining  ; jak nie ma co robi� po 4, to robimy pojedynczo

    
process_pixels:
    ; tu zaczyna si� g��wna robota - robimy 4 piksele na raz
    movd xmm1, dword ptr [rsi]    ; bierzemy pierwszy piksel
    movd xmm2, dword ptr [rsi+4]  ; bierzemy drugi piksel
    movd xmm10, dword ptr [rsi+8] ; bierzemy trzeci piksel
    
    ; zamieniamy ma�e liczby na wi�ksze bo tak �atwiej liczy� (rozszerzamy do 16 bajt�w)
    pxor xmm15, xmm15    ; robimy sobie zero do pomocy        
    punpcklbw xmm1, xmm15     ; robimy z malutkich liczb wi�ksze    
    punpcklbw xmm2, xmm15   ; to samo dla drugiego piksela
    punpcklbw xmm10, xmm15  ; i dla trzeciego

    ;robimy jeszcze wi�ksze liczby(rozszerzamy do 32 bajt�w)
    punpcklwd xmm1, xmm15         
    punpcklwd xmm2, xmm15
    punpcklwd xmm10, xmm15
    
    ; Konwersja int -> float
    cvtdq2ps xmm1, xmm1     ; teraz b�d� u�amki     
    cvtdq2ps xmm2, xmm2      ; bo na u�amkach lepiej si� liczy   
    cvtdq2ps xmm10, xmm10      ; dla wszystkich pikseli
    
    ; Liczymy jak szary ma by� ka�dy piksel
    mulps xmm1, xmm4   ; mno�ymy przez nasze liczby           
    mulps xmm2, xmm5  
    mulps xmm10, xmm6   
    
    ; sk�adamy wszystko do kupy
    addps xmm1, xmm2   ; dodajemy wszystkie kolory razem          
    addps xmm1, xmm10   ; �eby wyszed� jeden szary kolor
    
    ; Zaokr�glanie i ograniczenie zakresu
    addps xmm1, xmm9        ; dodajemy nasze liczby do zaokr�glania     
    maxps xmm1, xmm8         ; pilnujemy �eby nie by�o za ciemno     
    minps xmm1, xmm7         ; pilnujemy �eby nie by�o za jasno     
    
    ; Zamieniamy z powrotem na liczby ca�kowite
    cvtps2dq xmm1, xmm1           ; float -> int
    packssdw xmm1, xmm1           ; int -> word
    packuswb xmm1, xmm1           ; word -> byte
    
    ; Zapisujemy nasz szary kolor
    movd eax, xmm1
    
    ; Zapisujemy ka�dy piksel jako szary (trzy razy ten sam kolor)
    mov byte ptr [rdi], al        ; piksel 1
    mov byte ptr [rdi+1], al     ; trzy razy to samo bo RGB
    mov byte ptr [rdi+2], al    ; ale wszystkie takie same = szary
    shr eax, 8                   ; bierzemy nast�pny piksel
    mov byte ptr [rdi+3], al      ; piksel 2
    mov byte ptr [rdi+4], al
    mov byte ptr [rdi+5], al
    shr eax, 8
    mov byte ptr [rdi+6], al      ; piksel 3
    mov byte ptr [rdi+7], al
    mov byte ptr [rdi+8], al
    shr eax, 8
    mov byte ptr [rdi+9], al      ; piksel 4
    mov byte ptr [rdi+10], al
    mov byte ptr [rdi+11], al
    
    ;przesuwamy si� na nast�pne piksele
    add rsi, 12 ; przesuwamy si� w obrazku �r�d�owym
    add rdi, 12 ; przesuwamy si� w nowym obrazku
    
    dec ecx ; zmniejszamy licznik pikseli
    jnz process_pixels ; je�li jeszcze s� piksele to robimy dalej
    
process_remaining:
    ;Pozosta�e piksele
    and ebx, 3                    ; reszta z dzielenia przez 4
    test ebx, ebx
    jz cleanup
    
remaining_loop:
    ; zajmujemy si� pojedynczymi pikselami kt�re zosta�y
    movzx eax, byte ptr [rsi]   ; bierzemy czerwony (R) z piksela  
    cvtsi2ss xmm1, eax          ; zamieniamy na liczb� z przecinkiem
    mulss xmm1, xmm4            ; mno�ymy przez nasz� magiczn� liczb� dla czerwonego
    
    movzx eax, byte ptr [rsi+1]   
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm5
    addss xmm1, xmm2
    
    movzx eax, byte ptr [rsi+2]   
    cvtsi2ss xmm2, eax
    mulss xmm2, xmm6
    addss xmm1, xmm2
    
    ; poprawiamy nasz� liczb� �eby by�a �adna
    addss xmm1, xmm9    ; dodajemy liczb� do zaokr�glania
    maxss xmm1, xmm8    ; pilnujemy �eby nie by�o za ciemno
    minss xmm1, xmm7    ; pilnujemy �eby nie by�o za jasno
    
    ;zamieniamy na zwyk�� liczb� i zapisujemy
    cvtss2si eax, xmm1

    ; zapisujemy nasz szary kolor trzy razy (bo RGB)
    mov byte ptr [rdi], al
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    
    ;przesuwamy si� na nast�pny piksel
    add rsi, 3 ; przesuwamy si� w starym obrazku (3 bo RGB)
    add rdi, 3  ; przesuwamy si� w nowym obrazku (3 bo RGB)
   
   ;sprawdzamy czy to ju� wszystko
    dec ebx ; zmniejszamy licznik pikseli
    jnz remaining_loop  ; je�li zosta�y jeszcze jakie�, robimy dalej

;koniec programu - sprz�tamy po sobie  
cleanup:
    pop rbx ; zbieramy nasze rzeczy kt�re od�o�yli�my
    pop rdi ; ka�d� po kolei
    pop rsi ; tak jak uk�adali�my
    pop rbp ; na samym pocz�tku
    ret ; ko�czymy program

GrayscaleFilter ENDP
END